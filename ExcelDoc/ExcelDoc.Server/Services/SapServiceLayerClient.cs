using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Sap;
using ExcelDoc.Server.Services.Interfaces;
using ExcelDoc.Server.Localization;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Services;

public sealed class SapServiceLayerClient : ISapServiceLayerClient, IDisposable
{
    private const string RequestPayloadKey = "RequestPayload";
    private const string ResponseBodyKey = "ResponseBody";
    private static readonly JsonSerializerOptions SapJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null
    };
    private static readonly JsonSerializerOptions ProcessingJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SapServiceLayerClient> _logger;
    private readonly IMessageService _messageService;
    private readonly SapServiceLayerOptions _sapOptions;
    private readonly TokenBucketRateLimiter _rateLimiter;

    public SapServiceLayerClient(
        IHttpClientFactory httpClientFactory,
        IMessageService messageService,
        IOptions<ProcessingOptions> processingOptions,
        IOptions<SapServiceLayerOptions> sapOptions,
        ILogger<SapServiceLayerClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _messageService = messageService;
        _logger = logger;
        _sapOptions = sapOptions.Value;

        var permitsPerSecond = Math.Max(1, processingOptions.Value.SapRequestsPerSecond);
        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = 100,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokenLimit = permitsPerSecond,
            TokensPerPeriod = permitsPerSecond
        });
    }

    public async Task<SapSessionContext> LoginAsync(
        string database,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(_sapOptions.BaseUrl);
        using var loginContent = JsonContent.Create(
            new
            {
                CompanyDB = database,
                UserName = userName,
                Password = password
            },
            options: SapJsonOptions);
        using var response = await client.PostAsync(
            "Login",
            loginContent,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Login recusado pelo SAP Service Layer para usuário {UserName} e base {Database}. StatusCode={StatusCode}",
                userName,
                database,
                response.StatusCode);

            throw new UnauthorizedAccessException(
                ExtractSapMessage(responseBody) ?? "Usuário, senha ou base SAP inválidos.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var sessionId = root.TryGetProperty("SessionId", out var sessionIdElement)
            ? sessionIdElement.GetString()
            : null;
        var timeoutMinutes = root.TryGetProperty("SessionTimeout", out var timeoutElement) &&
                             timeoutElement.TryGetInt32(out var parsedTimeout)
            ? Math.Max(1, parsedTimeout)
            : 30;

        var cookieHeader = BuildCookieHeader(response, sessionId);
        if (string.IsNullOrWhiteSpace(cookieHeader))
        {
            throw new InvalidOperationException(
                "O SAP Service Layer autenticou o usuário, mas não retornou o cookie B1SESSION.");
        }

        return new SapSessionContext
        {
            ServiceLayerBaseUrl = NormalizeBaseAddress(_sapOptions.BaseUrl).ToString(),
            Database = database,
            UserName = userName,
            CookieHeader = cookieHeader,
            SessionTimeoutMinutes = timeoutMinutes,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(timeoutMinutes)
        };
    }

    public async Task LogoutAsync(
        SapSessionContext session,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            session,
            HttpMethod.Post,
            "Logout",
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug(
                "SAP Service Layer não confirmou logout. Base={Database} StatusCode={StatusCode}",
                session.Database,
                response.StatusCode);
        }
    }

    public async Task<HttpResponseMessage> SendAsync(
        SapSessionContext session,
        HttpMethod method,
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(
            session,
            method,
            endpoint,
            payload,
            cancellationToken,
            SapJsonOptions);
    }

    private async Task<HttpResponseMessage> SendAsync(
        SapSessionContext session,
        HttpMethod method,
        string endpoint,
        object? payload,
        CancellationToken cancellationToken,
        JsonSerializerOptions jsonOptions)
    {
        var client = CreateClient(session.ServiceLayerBaseUrl);
        var request = new HttpRequestMessage(
            method,
            NormalizeRelativeEndpoint(endpoint));
        request.Headers.TryAddWithoutValidation("Cookie", session.CookieHeader);

        if (payload is string json)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        else if (payload is not null)
        {
            request.Content = JsonContent.Create(payload, options: jsonOptions);
        }

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            session.ExpiresAtUtc = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? DateTime.UtcNow
                : DateTime.UtcNow.AddMinutes(session.SessionTimeoutMinutes);
            return response;
        }
        finally
        {
            request.Dispose();
            client.Dispose();
        }
    }

    public async Task<string> PostAsync(
        SapSessionContext session,
        string endpoint,
        string payload,
        CancellationToken cancellationToken = default)
    {
        using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException(
                _messageService.Get(MessageKeys.SapRequestReservationFailed));
        }

        using var response = await SendAsync(
            session,
            HttpMethod.Post,
            endpoint,
            payload,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return responseBody;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new SapSessionExpiredException(
                ExtractSapMessage(responseBody) ??
                _messageService.Get(MessageKeys.SapSessionExpired));
        }

        var exception = new SapServiceLayerException(
            ExtractSapMessage(responseBody) ?? _messageService.Get(MessageKeys.SapServiceLayerOperationFailed),
            response.StatusCode,
            responseBody);
        exception.Data[RequestPayloadKey] = payload;
        exception.Data[ResponseBodyKey] = responseBody;
        throw exception;
    }

    public async Task<string> PostProcessamentoAsync(
        SapSessionContext session,
        string endpoint,
        object payload,
        CancellationToken cancellationToken = default)
    {
        using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException(
                _messageService.Get(MessageKeys.SapRequestReservationFailed));
        }

        var requestPayload = JsonSerializer.Serialize(payload, ProcessingJsonOptions);
        using var response = await SendAsync(
            session,
            HttpMethod.Post,
            endpoint,
            payload,
            cancellationToken,
            ProcessingJsonOptions);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return BuildProcessamentoResponse(responseBody);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new SapSessionExpiredException(
                ExtractSapMessage(responseBody) ??
                _messageService.Get(MessageKeys.SapSessionExpired));
        }

        var exception = new SapServiceLayerException(
            ExtractSapMessage(responseBody) ?? _messageService.Get(MessageKeys.SapServiceLayerOperationFailed),
            response.StatusCode,
            responseBody);
        exception.Data[RequestPayloadKey] = requestPayload;
        exception.Data[ResponseBodyKey] = responseBody;

        throw exception;
    }

    private HttpClient CreateClient(string baseUrl)
    {
        var client = _httpClientFactory.CreateClient("sap-service-layer");
        client.BaseAddress = NormalizeBaseAddress(baseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    private string BuildProcessamentoResponse(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                _messageService.Get(MessageKeys.SapProcessingResponseInvalid));
        }

        var documentLines = root.TryGetProperty("DocumentLines", out var lines) &&
                            lines.ValueKind == JsonValueKind.Array
            ? lines.EnumerateArray().Select(line => (object)new
            {
                ItemCode = GetJsonProperty(line, "ItemCode"),
                Quantity = GetJsonProperty(line, "Quantity"),
                Price = GetJsonProperty(line, "Price")
            }).ToArray()
            : Array.Empty<object>();

        return JsonSerializer.Serialize(new
        {
            DocEntry = GetJsonProperty(root, "DocEntry"),
            DocNum = GetJsonProperty(root, "DocNum"),
            CardCode = GetJsonProperty(root, "CardCode"),
            CardName = GetJsonProperty(root, "CardName"),
            SequenceSerial = GetJsonProperty(root, "SequenceSerial"),
            DocDate = GetJsonProperty(root, "DocDate"),
            DocumentLines = documentLines
        }, ProcessingJsonOptions);
    }

    private static JsonElement? GetJsonProperty(JsonElement source, string propertyName) =>
        source.ValueKind == JsonValueKind.Object && source.TryGetProperty(propertyName, out var value)
            ? value
            : null;

    private static string BuildCookieHeader(
        HttpResponseMessage response,
        string? sessionId)
    {
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values
                .Select(value => value.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim())
                .Where(value =>
                    value.StartsWith("B1SESSION=", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("ROUTEID=", StringComparison.OrdinalIgnoreCase))
                .ToList()
            : [];

        if (!cookies.Any(value =>
                value.StartsWith("B1SESSION=", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(sessionId))
        {
            cookies.Insert(0, $"B1SESSION={sessionId}");
        }

        return string.Join("; ", cookies);
    }

    private static string? ExtractSapMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                if (message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString();
                }

                if (message.TryGetProperty("value", out var value))
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // The status code remains available to the caller even for non-JSON responses.
        }

        return null;
    }

    private static Uri NormalizeBaseAddress(string linkServiceLayer)
    {
        var normalized = linkServiceLayer.Trim().TrimEnd('/');

        if (!normalized.Contains("/b1s/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"{normalized}/b1s/v1";
        }

        return new Uri($"{normalized}/", UriKind.Absolute);
    }

    private static string NormalizeRelativeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException(
                "O endpoint da Service Layer é obrigatório.",
                nameof(endpoint));
        }

        var normalized = endpoint.Trim();
        if (normalized.StartsWith('/') ||
            normalized.StartsWith('\\') ||
            Uri.TryCreate(normalized, UriKind.Absolute, out _) ||
            normalized.Split('?', 2)[0]
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException(
                "O endpoint da Service Layer deve ser um caminho relativo seguro.");
        }

        return Uri.TryCreate(normalized, UriKind.Relative, out _)
            ? normalized
            : throw new InvalidOperationException(
                "O endpoint da Service Layer é inválido.");
    }

    public void Dispose()
    {
        _rateLimiter.Dispose();
    }
}
