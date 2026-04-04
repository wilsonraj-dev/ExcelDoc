using ExcelDoc.Server.Background;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface ISapServiceLayerClient
    {
        Task<SapSession> LoginAsync(Configuracao configuracao, CancellationToken cancellationToken = default);

        Task<string> PostAsync(Configuracao configuracao, SapSession session, string endpoint, string payload, CancellationToken cancellationToken = default);
    }
}
