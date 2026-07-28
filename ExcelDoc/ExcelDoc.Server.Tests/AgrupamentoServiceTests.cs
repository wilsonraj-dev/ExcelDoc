using ExcelDoc.Server.Background;
using ExcelDoc.Server.Services;

namespace ExcelDoc.Server.Tests;

public sealed class AgrupamentoServiceTests
{
    [Fact]
    public void AgruparPorIdExcel_UsesSecondHeaderRowWhenBothStartWithHash()
    {
        var rows = new[]
        {
            CreateRow(1, "#", "DocDate"),
            CreateRow(2, "#", "Data de lançamento"),
            CreateRow(3, "1", "01/04/2026"),
            CreateRow(4, "1", "01/04/2026"),
            CreateRow(5, "2", "02/04/2026")
        };
        var service = new AgrupamentoService(new StubMessageService());

        var result = service.AgruparPorIdExcel(rows);

        Assert.Collection(
            result,
            group =>
            {
                Assert.Equal(1, group.IdExcel);
                Assert.Equal([3, 4], group.Rows.Select(row => row.RowNumber));
            },
            group =>
            {
                Assert.Equal(2, group.IdExcel);
                Assert.Equal([5], group.Rows.Select(row => row.RowNumber));
            });
    }

    private static ExcelRowData CreateRow(
        int rowNumber,
        string idExcel,
        string value) =>
        new()
        {
            RowNumber = rowNumber,
            Values = new Dictionary<int, string?>
            {
                [1] = idExcel,
                [2] = value
            }
        };
}
