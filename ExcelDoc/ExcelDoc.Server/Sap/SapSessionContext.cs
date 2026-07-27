namespace ExcelDoc.Server.Sap;

public sealed class SapSessionContext
{
    public string SessionKey { get; init; } = Guid.NewGuid().ToString("N");

    public string ServiceLayerBaseUrl { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string CookieHeader { get; init; } = string.Empty;

    public int SessionTimeoutMinutes { get; init; } = 30;

    public DateTime ExpiresAtUtc { get; set; }
}
