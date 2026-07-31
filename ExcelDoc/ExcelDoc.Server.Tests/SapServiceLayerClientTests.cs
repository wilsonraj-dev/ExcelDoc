using ExcelDoc.Server.Options;
using ExcelDoc.Server.Sap;
using ExcelDoc.Server.Services;

namespace ExcelDoc.Server.Tests;

public sealed class SapServiceLayerClientTests
{
    [Fact]
    public async Task PostProcessamentoAsync_RejectsAbsoluteEndpointBeforeResolvingConnection()
    {
        using var client = CreateClient();
        var session = CreateSession();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.PostProcessamentoAsync(
                session,
                "https://untrusted.example.test/b1s/v1/Orders",
                new { }));

        Assert.Contains("caminho relativo seguro", exception.Message);
    }

    [Fact]
    public async Task PostProcessamentoAsync_RequiresLongLivedB1SlayerConnection()
    {
        using var client = CreateClient();
        var session = CreateSession();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.PostProcessamentoAsync(session, "Orders", new { }));

        Assert.Contains("B1SLayer", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_HonorsAlreadyCanceledTokenWithoutCallingSap()
    {
        using var client = CreateClient();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.LoginAsync(
                "SBOPROD",
                "manager",
                "sap-password",
                cancellation.Token));
    }

    private static SapSessionContext CreateSession() =>
        new()
        {
            ServiceLayerBaseUrl = "https://sap.example.test:50000/b1s/v1",
            Database = "SBOPROD",
            UserName = "manager",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        };

    private static SapServiceLayerClient CreateClient()
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
            new StubMessageService(),
            processingOptions,
            sapOptions);
    }
}
