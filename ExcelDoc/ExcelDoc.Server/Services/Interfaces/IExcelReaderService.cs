using ExcelDoc.Server.Background;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IExcelReaderService
    {
        Task<IReadOnlyCollection<ExcelRowData>> ReadRowsAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
