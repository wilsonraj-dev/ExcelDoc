using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using B1SLayer;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Sap;
using ExcelDoc.Server.Services.Interfaces;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Services;

public sealed class SapServiceLayerClient : ISapServiceLayerClient, IDisposable
{
    private const int PortugueseLanguageCode = 29;
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
    private readonly ILogger<SapServiceLayerClient> _logger;
    private readonly IMessageService _messageService;
    private readonly SapServiceLayerOptions _sapOptions;
    private readonly TokenBucketRateLimiter _rateLimiter;

    public SapServiceLayerClient(
        IMessageService messageService,
        IOptions<ProcessingOptions> processingOptions,
        IOptions<SapServiceLayerOptions> sapOptions,
        ILogger<SapServiceLayerClient> logger)
    {
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
        cancellationToken.ThrowIfCancellationRequested();

        var connection = new SLConnection(
            NormalizeBaseAddress(_sapOptions.BaseUrl),
            database,
            userName,
            password,
            PortugueseLanguageCode);

        try
        {
            var login = await connection.LoginAsync();
            var timeoutMinutes = Math.Max(1, login.SessionTimeout);

            return new SapSessionContext
            {
                Connection = connection,
                ServiceLayerBaseUrl = connection.ServiceLayerRoot.ToString(),
                Database = database,
                UserName = userName,
                SessionTimeoutMinutes = timeoutMinutes,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(timeoutMinutes)
            };
        }
        catch (Exception exception) when (IsServiceLayerException(exception))
        {
            var error = await GetErrorAsync(exception);
            connection.Client.Dispose();

            _logger.LogWarning(
                exception,
                "Login recusado pelo SAP Service Layer para usuário {UserName} e base {Database}. StatusCode={StatusCode}",
                userName,
                database,
                error.StatusCode);

            if (error.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    error.Message ?? "Usuário, senha ou base SAP inválidos.",
                    exception);
            }

            throw new InvalidOperationException(
                error.Message ?? "Não foi possível conectar ao SAP Service Layer.",
                exception);
        }
        catch
        {
            connection.Client.Dispose();
            throw;
        }
    }

    public async Task LogoutAsync(
        SapSessionContext session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var connection = session.GetRequiredConnection();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await connection.LogoutAsync();
        }
        catch (Exception exception) when (IsServiceLayerException(exception))
        {
            var error = await GetErrorAsync(exception);
            _logger.LogDebug(
                exception,
                "SAP Service Layer não confirmou logout. Base={Database} StatusCode={StatusCode}",
                session.Database,
                error.StatusCode);
        }
        finally
        {
            session.ExpiresAtUtc = DateTime.UtcNow;
            connection.Client.Dispose();
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
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(method);
        cancellationToken.ThrowIfCancellationRequested();

        var resource = NormalizeRelativeEndpoint(endpoint);
        var request = session
            .GetRequiredConnection()
            .Request(resource)
            .WithJsonSerializerOptions(jsonOptions)
            .WithTimeout(_sapOptions.RequestTimeoutSeconds);

        try
        {
            var (statusCode, responseBody) = await ExecuteAsync(request, method, payload);
            session.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(session.SessionTimeoutMinutes);
            return CreateResponse(statusCode, responseBody);
        }
        catch (Exception exception) when (IsServiceLayerException(exception))
        {
            var error = await GetErrorAsync(exception);
            session.ExpiresAtUtc = error.StatusCode == HttpStatusCode.Unauthorized
                ? DateTime.UtcNow
                : DateTime.UtcNow.AddMinutes(session.SessionTimeoutMinutes);

            return CreateResponse(
                error.StatusCode ?? HttpStatusCode.InternalServerError,
                error.ResponseBody ?? error.Message ?? string.Empty);
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

        using var document = JsonDocument.Parse(payload);
        using var response = await SendAsync(
            session,
            HttpMethod.Post,
            endpoint,
            document.RootElement.Clone(),
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return responseBody;
        }

        ThrowServiceLayerException(response.StatusCode, responseBody, payload);
        return string.Empty;
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

        ThrowServiceLayerException(response.StatusCode, responseBody, requestPayload);
        return string.Empty;
    }

    private static async Task<(HttpStatusCode StatusCode, string ResponseBody)> ExecuteAsync(
        SLRequest request,
        HttpMethod method,
        object? payload)
    {
        if (method == HttpMethod.Get)
        {
            return (HttpStatusCode.OK, await request.GetStringAsync());
        }

        if (method == HttpMethod.Post)
        {
            var responseBody = payload is null
                ? await request.PostReceiveStringAsync()
                : await request.PostReceiveStringAsync(payload);
            return (HttpStatusCode.Created, responseBody);
        }

        if (method == HttpMethod.Patch)
        {
            await request.PatchAsync(payload ?? new { });
            return (HttpStatusCode.NoContent, string.Empty);
        }

        if (method == HttpMethod.Delete)
        {
            await request.DeleteAsync();
            return (HttpStatusCode.NoContent, string.Empty);
        }

        throw new NotSupportedException(
            $"O método HTTP {method.Method} não é suportado para chamadas ao SAP Service Layer.");
    }

    private void ThrowServiceLayerException(
        HttpStatusCode statusCode,
        string responseBody,
        string requestPayload)
    {
        if (statusCode == HttpStatusCode.Unauthorized)
        {
            throw new SapSessionExpiredException(
                ExtractSapMessage(responseBody) ??
                _messageService.Get(MessageKeys.SapSessionExpired));
        }

        var exception = new SapServiceLayerException(
            ExtractSapMessage(responseBody) ??
            _messageService.Get(MessageKeys.SapServiceLayerOperationFailed),
            statusCode,
            responseBody);
        exception.Data[RequestPayloadKey] = requestPayload;
        exception.Data[ResponseBodyKey] = responseBody;
        throw exception;
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

    private static bool IsServiceLayerException(Exception exception) =>
        FindFlurlException(exception) is not null ||
        exception is SLException;

    private static async Task<ServiceLayerError> GetErrorAsync(Exception exception)
    {
        var flurlException = FindFlurlException(exception);
        var rawStatusCode = flurlException?.Call.Response?.StatusCode;
        var statusCode = rawStatusCode.HasValue
            ? (HttpStatusCode?)rawStatusCode.Value
            : null;
        string? responseBody = null;

        if (flurlException is not null)
        {
            try
            {
                responseBody = await flurlException.GetResponseStringAsync();
            }
            catch
            {
                // A mensagem tipada da B1SLayer permanece disponível abaixo.
            }
        }

        return new ServiceLayerError(
            statusCode,
            responseBody,
            ExtractSapMessage(responseBody) ?? exception.Message);
    }

    private static FlurlHttpException? FindFlurlException(Exception exception)
    {
        if (exception is FlurlHttpException flurlException)
        {
            return flurlException;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.Flatten().InnerExceptions)
            {
                var match = FindFlurlException(innerException);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return exception.InnerException is null
            ? null
            : FindFlurlException(exception.InnerException);
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string responseBody) =>
        new(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

    private static string? ExtractSapMessage(string? responseBody)
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
            // A mensagem da exceção B1SLayer será usada quando a resposta não for JSON.
        }

        return null;
    }

    private static string NormalizeBaseAddress(string serviceLayerUrl)
    {
        var normalized = serviceLayerUrl.Trim().TrimEnd('/');
        if (!normalized.Contains("/b1s/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"{normalized}/b1s/v1";
        }

        return normalized;
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

    private sealed record ServiceLayerError(
        HttpStatusCode? StatusCode,
        string? ResponseBody,
        string? Message);
}
