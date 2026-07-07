using ExcelDoc.Server.Background;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IDocumentoUnicoService
    {
        string BuildIdDocumentoUnico(
            Processamento processamento,
            ExcelDocumentGroup group,
            IDictionary<string, object?> payload);
    }
}
