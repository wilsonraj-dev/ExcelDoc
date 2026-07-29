using System.Net;
using System.Text;
using System.Text.Json;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Sap;
using ExcelDoc.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExcelDoc.Server.Tests;

public sealed class SapServiceLayerClientTests
{
    [Fact]
    public async Task LoginAsync_SerializesExactSapPropertyNames()
    {
        using var handler = new RecordingHttpMessageHandler();
        var factory = new RecordingHttpClientFactory(handler);
        using var client = CreateClient(factory);

        var session = await client.LoginAsync(
            "SBOPROD",
            "manager",
            "sap-password");

        Assert.Equal("sap-service-layer", factory.RequestedName);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(
            new Uri("https://sap.example.test:50000/b1s/v1/Login"),
            handler.RequestUri);
        Assert.NotNull(handler.RequestBody);

        using var payload = JsonDocument.Parse(handler.RequestBody);
        var root = payload.RootElement;
        Assert.Equal(
            new[] { "CompanyDB", "UserName", "Password" },
            root.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal("SBOPROD", root.GetProperty("CompanyDB").GetString());
        Assert.Equal("manager", root.GetProperty("UserName").GetString());
        Assert.Equal("sap-password", root.GetProperty("Password").GetString());
        Assert.Equal("B1SESSION=sap-session", session.CookieHeader);
    }

    [Fact]
    public async Task SendAsync_RejectsAbsoluteEndpointWithoutSendingRequest()
    {
        using var handler = new RecordingHttpMessageHandler();
        var factory = new RecordingHttpClientFactory(handler);
        using var client = CreateClient(factory);
        var session = new SapSessionContext
        {
            ServiceLayerBaseUrl = "https://sap.example.test:50000/b1s/v1/",
            Database = "SBOPROD",
            UserName = "manager",
            CookieHeader = "B1SESSION=sap-session",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SendAsync(
                session,
                HttpMethod.Get,
                "https://untrusted.example.test/b1s/v1/Users"));

        Assert.Contains("caminho relativo seguro", exception.Message);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendAsync_PreservesExactMetadataPropertyNames()
    {
        using var handler = new RecordingHttpMessageHandler();
        var factory = new RecordingHttpClientFactory(handler);
        using var client = CreateClient(factory);
        var session = CreateSession();

        using var response = await client.SendAsync(
            session,
            HttpMethod.Post,
            "UserTablesMD",
            new
            {
                TableName = "EXD_TEST",
                TableType = "bott_MasterData"
            });

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(
            new[] { "TableName", "TableType" },
            payload.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .ToArray());
    }

    [Fact]
    public async Task PostProcessamentoAsync_SerializesPayloadBeforeSending()
    {
        using var handler = new RecordingHttpMessageHandler();
        var factory = new RecordingHttpClientFactory(handler);
        using var client = CreateClient(factory);

        await client.PostProcessamentoAsync(
            CreateSession(),
            "Invoices",
            new
            {
                CardCode = "C20000",
                Comments = (string?)null
            });

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(new Uri("https://sap.example.test:50000/b1s/v1/Invoices"), handler.RequestUri);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("C20000", payload.RootElement.GetProperty("CardCode").GetString());
        Assert.False(payload.RootElement.TryGetProperty("Comments", out _));
    }

    [Fact]
    public async Task PostProcessamentoAsync_ReturnsOnlyProcessingResponseFields()
    {
        using var handler = new RecordingHttpMessageHandler(
            """
            {
              "DocEntry": 12,
              "DocNum": 34,
              "CardCode": "C20000",
              "CardName": "Cliente",
              "SequenceSerial": 56,
              "DocDate": "2026-07-29",
              "Comments": "Não deve retornar",
              "DocumentLines": [
                {
                  "ItemCode": "A0001",
                  "Quantity": 2,
                  "Price": 19.9,
                  "WarehouseCode": "01"
                }
              ]
            }
            """);
        var factory = new RecordingHttpClientFactory(handler);
        using var client = CreateClient(factory);

        var result = await client.PostProcessamentoAsync(
            CreateSession(),
            "Invoices",
            new { CardCode = "C20000" });

        using var response = JsonDocument.Parse(result);
        Assert.Equal(
            ["DocEntry", "DocNum", "CardCode", "CardName", "SequenceSerial", "DocDate", "DocumentLines"],
            response.RootElement.EnumerateObject().Select(property => property.Name).ToArray());

        var line = Assert.Single(response.RootElement.GetProperty("DocumentLines").EnumerateArray());
        Assert.Equal(
            ["ItemCode", "Quantity", "Price"],
            line.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal("A0001", line.GetProperty("ItemCode").GetString());
    }

    private static SapSessionContext CreateSession() =>
        new()
        {
            ServiceLayerBaseUrl = "https://sap.example.test:50000/b1s/v1/",
            Database = "SBOPROD",
            UserName = "manager",
            CookieHeader = "B1SESSION=sap-session",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        };

    private static SapServiceLayerClient CreateClient(IHttpClientFactory factory)
    {
        var processingOptions =
            Microsoft.Extensions.Options.Options.Create(new ProcessingOptions
            {
                SapRequestsPerSecond = 10
            });
        var sapOptions =
            Microsoft.Extensions.Options.Options.Create(new SapServiceLayerOptions
            {
                BaseUrl = "https://sap.example.test:50000/b1s/v1",
                RequestTimeoutSeconds = 30,
                Bases =
                [
                    new SapBaseOptions
                    {
                        Database = "SBOPROD",
                        Description = "Produção"
                    }
                ]
            });

        return new SapServiceLayerClient(
            factory,
            new StubMessageService(),
            processingOptions,
            sapOptions,
            NullLogger<SapServiceLayerClient>.Instance);
    }

    private sealed class RecordingHttpClientFactory(
        RecordingHttpMessageHandler handler) : IHttpClientFactory
    {
        public string? RequestedName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            RequestedName = name;
            return new HttpClient(handler, disposeHandler: false);
        }
    }

    private sealed class RecordingHttpMessageHandler(string? responseBody = null) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    responseBody ?? """{"SessionId":"sap-session","SessionTimeout":30}""",
                    Encoding.UTF8,
                    "application/json"),
                RequestMessage = request
            };
            response.Headers.TryAddWithoutValidation(
                "Set-Cookie",
                "B1SESSION=sap-session; Path=/; Secure; HttpOnly");
            return response;
        }
    }
}
