using B1SLayer;

namespace ExcelDoc.Server.Sap;

public sealed class SapSessionContext
{
    public string SessionKey { get; init; } = Guid.NewGuid().ToString("N");

    public string ServiceLayerBaseUrl { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public SLConnection? Connection { get; init; }

    public int SessionTimeoutMinutes { get; init; } = 30;

    public DateTime ExpiresAtUtc { get; set; }

    public SLConnection GetRequiredConnection() =>
        Connection ?? throw new InvalidOperationException(
            "A conexão B1SLayer não está disponível para esta sessão SAP.");
}
