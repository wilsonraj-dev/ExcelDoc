namespace ExcelDoc.Server.Background
{
    public class ExcelDocumentGroup
    {
        public int IdExcel { get; set; }

        public IReadOnlyList<ExcelRowData> Rows { get; set; } = [];
    }
}
