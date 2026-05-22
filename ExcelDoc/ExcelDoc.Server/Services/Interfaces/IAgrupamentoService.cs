using ExcelDoc.Server.Background;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IAgrupamentoService
    {
        IReadOnlyList<IReadOnlyList<ExcelRowData>> AgruparLinhas(
            IReadOnlyCollection<ExcelRowData> rows,
            IReadOnlyCollection<int> colunasAgrupamento);
    }
}
