using ExcelDoc.Server.Background;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IExcelReaderService
    {
        Task<IReadOnlyList<string>> ReadFirstRowAsync(Stream stream, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ExcelRowData>> ReadRowsAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
