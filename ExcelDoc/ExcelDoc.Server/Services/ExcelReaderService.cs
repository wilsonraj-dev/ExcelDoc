using ClosedXML.Excel;
using ExcelDoc.Server.Background;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class ExcelReaderService : IExcelReaderService
    {
        public Task<IReadOnlyCollection<ExcelRowData>> ReadRowsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.Run<IReadOnlyCollection<ExcelRowData>>(() =>
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();
                var rows = new List<ExcelRowData>();

                foreach (var row in worksheet.RowsUsed())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (row.IsEmpty())
                    {
                        continue;
                    }

                    var values = row.CellsUsed()
                        .ToDictionary(cell => cell.Address.ColumnNumber, cell => cell.GetFormattedString());

                    rows.Add(new ExcelRowData
                    {
                        RowNumber = row.RowNumber(),
                        Values = values
                    });
                }

                return rows;
            }, cancellationToken);
        }
    }
}
