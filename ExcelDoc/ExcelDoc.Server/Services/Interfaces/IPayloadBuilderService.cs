using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IPayloadBuilderService
    {
        IDictionary<string, object?> BuildPayload(Documento documento, Mapeamento mapeamento, IReadOnlyDictionary<int, string?> rowValues);
    }
}
