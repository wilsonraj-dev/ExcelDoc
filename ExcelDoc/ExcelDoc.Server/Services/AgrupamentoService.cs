using ExcelDoc.Server.Background;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class AgrupamentoService : IAgrupamentoService
    {
        public IReadOnlyList<IReadOnlyList<ExcelRowData>> AgruparLinhas(
            IReadOnlyCollection<ExcelRowData> rows,
            IReadOnlyCollection<int> colunasAgrupamento)
        {
            if (colunasAgrupamento.Count == 0)
            {
                return rows.Select(r => (IReadOnlyList<ExcelRowData>)new List<ExcelRowData> { r }).ToList();
            }

            var keyColumns = colunasAgrupamento.OrderBy(x => x).ToList();

            var groups = new List<IReadOnlyList<ExcelRowData>>();
            var groupDict = new Dictionary<string, List<ExcelRowData>>(StringComparer.Ordinal);
            var groupOrder = new List<string>();

            foreach (var row in rows)
            {
                var keyParts = keyColumns.Select(col =>
                {
                    row.Values.TryGetValue(col, out var val);
                    return val ?? string.Empty;
                });

                var key = string.Join("||", keyParts);

                if (!groupDict.TryGetValue(key, out var list))
                {
                    list = new List<ExcelRowData>();
                    groupDict[key] = list;
                    groupOrder.Add(key);
                }

                list.Add(row);
            }

            foreach (var key in groupOrder)
            {
                groups.Add(groupDict[key]);
            }

            return groups;
        }
    }
}
