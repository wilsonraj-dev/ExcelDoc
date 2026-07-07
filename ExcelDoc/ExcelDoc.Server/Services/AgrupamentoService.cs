using ExcelDoc.Server.Background;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class AgrupamentoService : IAgrupamentoService
    {
        private const string IdExcelHeaderName = "#";
        private const int HeaderRowsToInspect = 2;

        public IReadOnlyList<ExcelDocumentGroup> AgruparPorIdExcel(
            IReadOnlyCollection<ExcelRowData> rows)
        {
            var orderedRows = rows.OrderBy(r => r.RowNumber).ToList();

            if (orderedRows.Count == 0)
            {
                throw new ExcelImportValidationException("A planilha não possui linhas preenchidas.");
            }

            var idExcelHeader = FindIdExcelHeader(orderedRows)
                ?? throw new ExcelImportValidationException("A coluna obrigatória '#' não foi encontrada nas linhas de cabeçalho da planilha.");

            var dataRows = orderedRows
                .Where(row => row.RowNumber > idExcelHeader.RowNumber)
                .ToList();

            if (dataRows.Count == 0)
            {
                throw new ExcelImportValidationException("A planilha não possui linhas de dados para processamento.");
            }

            var groupsById = new Dictionary<int, List<ExcelRowData>>();
            var groupOrder = new List<int>();

            foreach (var row in dataRows)
            {
                var idExcel = ReadIdExcel(row, idExcelHeader.ColumnNumber);

                if (!groupsById.TryGetValue(idExcel, out var groupRows))
                {
                    groupRows = new List<ExcelRowData>();
                    groupsById[idExcel] = groupRows;
                    groupOrder.Add(idExcel);
                }

                groupRows.Add(row);
            }

            return groupOrder
                .Select(id => new ExcelDocumentGroup
                {
                    IdExcel = id,
                    Rows = groupsById[id]
                })
                .ToList();
        }

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

        private static IdExcelHeader? FindIdExcelHeader(IReadOnlyList<ExcelRowData> rows)
        {
            foreach (var row in rows.Take(HeaderRowsToInspect))
            {
                foreach (var value in row.Values)
                {
                    if (string.Equals(value.Value?.Trim(), IdExcelHeaderName, StringComparison.Ordinal))
                    {
                        return new IdExcelHeader(row.RowNumber, value.Key);
                    }
                }
            }

            return null;
        }

        private static int ReadIdExcel(ExcelRowData row, int columnNumber)
        {
            if (!row.Values.TryGetValue(columnNumber, out var rawValue) ||
                string.IsNullOrWhiteSpace(rawValue))
            {
                throw new ExcelImportValidationException(
                    $"Valor da coluna '#' vazio na linha {row.RowNumber}.",
                    row.RowNumber);
            }

            var value = rawValue.Trim();

            if (!int.TryParse(value, out var idExcel))
            {
                throw new ExcelImportValidationException(
                    $"Valor inválido na coluna '#' na linha {row.RowNumber}: '{value}'. Informe um número inteiro.",
                    row.RowNumber);
            }

            return idExcel;
        }

        private sealed record IdExcelHeader(int RowNumber, int ColumnNumber);
    }
}
