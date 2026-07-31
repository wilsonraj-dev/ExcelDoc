using B1SLayer;
using ExcelDoc.Server.Sap;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Background;

public sealed class SapSessionCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);

    private readonly ISapServiceLayerClient _sapClient;
    private readonly ISapSessionStore _sessionStore;
    private readonly ILogger<SapSessionCleanupHostedService> _logger;

    public SapSessionCleanupHostedService(
        ISapServiceLayerClient sapClient,
        ISapSessionStore sessionStore,
        ILogger<SapSessionCleanupHostedService> logger)
    {
        _sapClient = sapClient;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await KeepActiveJobSessionsAliveAsync(stoppingToken);

                foreach (var session in _sessionStore.RemoveExpired())
                {
                    try
                    {
                        await _sapClient.LogoutAsync(session, stoppingToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogDebug(
                            exception,
                            "Falha no logout de uma sessão SAP expirada da base {Database}.",
                            session.Database);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal do host.
        }
    }

    private async Task KeepActiveJobSessionsAliveAsync(
        CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;
        foreach (var session in _sessionStore.GetActiveJobSessions())
        {
            var keepAliveThreshold = TimeSpan.FromMinutes(
                Math.Max(0.5, session.SessionTimeoutMinutes / 2d));
            if (session.ExpiresAtUtc - now > keepAliveThreshold)
            {
                continue;
            }

            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                await session
                    .GetRequiredConnection()
                    .Request($"{SapUdtSchema.Schema}?$top=1&$select=Code")
                    .WithTimeout(session.RequestTimeoutSeconds)
                    .GetStringAsync();
                session.RenewExpiration();
            }
            catch (Exception exception) when (
                SapServiceLayerErrors.IsServiceLayerException(exception))
            {
                var error = await SapServiceLayerErrors.ReadAsync(exception);
                SapServiceLayerErrors.UpdateSessionExpiration(session, error.StatusCode);
                _logger.LogWarning(
                    exception,
                    "Falha no keep-alive da sessão SAP do usuário {UserName}. Status: {StatusCode}.",
                    session.UserName,
                    error.StatusCode);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Falha no keep-alive da sessão SAP do usuário {UserName}.",
                    session.UserName);
            }
        }
    }
}
