using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface ISapServiceLayerClient
    {
        Task<SapSessionContext> LoginAsync(
            string database,
            string userName,
            string password,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            SapSessionContext session,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> SendAsync(
            SapSessionContext session,
            HttpMethod method,
            string endpoint,
            object? payload = null,
            CancellationToken cancellationToken = default);

        Task<string> PostAsync(
            SapSessionContext session,
            string endpoint,
            string payload,
            CancellationToken cancellationToken = default);
    }
}
