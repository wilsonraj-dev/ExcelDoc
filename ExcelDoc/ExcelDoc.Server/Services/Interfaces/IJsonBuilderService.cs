using ExcelDoc.Server.Background;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IJsonBuilderService
    {
        IDictionary<string, object?> BuildDocumentPayload(
            PerfilMapeamento perfil,
            IReadOnlyList<ExcelRowData> groupRows);
    }
}
