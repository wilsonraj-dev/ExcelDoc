using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using ExcelDoc.Server.Background;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Services
{
    public class SapServiceLayerClient : ISapServiceLayerClient, IDisposable
    {
        private const string RequestPayloadKey = "RequestPayload";
        private const string ResponseBodyKey = "ResponseBody";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SapServiceLayerClient> _logger;
        private readonly TokenBucketRateLimiter _rateLimiter;

        public SapServiceLayerClient(
            IHttpClientFactory httpClientFactory,
            IOptions<ProcessingOptions> options,
            ILogger<SapServiceLayerClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            var permitsPerSecond = Math.Max(1, options.Value.SapRequestsPerSecond);
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

        public async Task<SapSession> LoginAsync(Configuracao configuracao, CancellationToken cancellationToken = default)
        {
            using var client = _httpClientFactory.CreateClient("sap-service-layer");
            client.BaseAddress = NormalizeBaseAddress(configuracao.LinkServiceLayer);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var request = new
            {
                CompanyDB = configuracao.Database,
                UserName = configuracao.UsuarioSAP,
                Password = configuracao.SenhaSAP
            };

            var requestJson = JsonSerializer.Serialize(request);
            var requestLogJson = JsonSerializer.Serialize(new
            {
                request.CompanyDB,
                request.UserName,
                Password = "***"
            });

            var response = await client.PostAsync("Login", new StringContent(requestJson, Encoding.UTF8, "application/json"), cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Falha no login do SAP. StatusCode={StatusCode} Body={Body}", response.StatusCode, responseContent);
                throw CreateHttpRequestException($"Falha ao autenticar no SAP Service Layer: {responseContent}", requestLogJson, responseContent);
            }

            using var document = JsonDocument.Parse(responseContent);
            var sessionId = document.RootElement.TryGetProperty("SessionId", out var sessionIdProperty)
                ? sessionIdProperty.GetString()
                : null;

            var cookieHeader = string.Join("; ", response.Headers.TryGetValues("Set-Cookie", out var cookies)
                ? cookies.Select(x => x.Split(';', StringSplitOptions.RemoveEmptyEntries)[0])
                : Array.Empty<string>());

            if (string.IsNullOrWhiteSpace(cookieHeader) && !string.IsNullOrWhiteSpace(sessionId))
            {
                cookieHeader = $"B1SESSION={sessionId}";
            }

            if (string.IsNullOrWhiteSpace(cookieHeader))
            {
                throw new InvalidOperationException("SAP Service Layer não retornou cookie de autenticação.");
            }

            return new SapSession { CookieHeader = cookieHeader };
        }

        public async Task<string> PostAsync(Configuracao configuracao, SapSession session, string endpoint, string payload, CancellationToken cancellationToken = default)
        {
            using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
            if (!lease.IsAcquired)
            {
                throw new InvalidOperationException("Não foi possível obter permissão para envio ao SAP.");
            }

            using var client = _httpClientFactory.CreateClient("sap-service-layer");
            client.BaseAddress = NormalizeBaseAddress(configuracao.LinkServiceLayer);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Cookie", session.CookieHeader);

            var response = await client.PostAsync(endpoint.TrimStart('/'), new StringContent(payload, Encoding.UTF8, "application/json"), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro no envio ao SAP. Endpoint={Endpoint} StatusCode={StatusCode} Body={Body}", endpoint, response.StatusCode, body);
                throw CreateHttpRequestException($"Erro no SAP Service Layer: {body}", payload, body);
            }

            return body;
        }

        private static HttpRequestException CreateHttpRequestException(string message, string requestPayload, string responseBody)
        {
            var exception = new HttpRequestException(message);
            exception.Data[RequestPayloadKey] = requestPayload;
            exception.Data[ResponseBodyKey] = responseBody;
            return exception;
        }

        private static Uri NormalizeBaseAddress(string linkServiceLayer)
        {
            var normalized = linkServiceLayer.TrimEnd('/');

            if (!normalized.Contains("/b1s/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = $"{normalized}/b1s/v1";
            }

            normalized = $"{normalized}/";
            return new Uri(normalized, UriKind.Absolute);
        }

        public void Dispose()
        {
            _rateLimiter.Dispose();
        }
    }
}
