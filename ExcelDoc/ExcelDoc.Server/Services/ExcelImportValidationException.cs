namespace ExcelDoc.Server.Services
{
    public class ExcelImportValidationException : InvalidOperationException
    {
        public ExcelImportValidationException(string message, int? rowNumber = null)
            : base(message)
        {
            RowNumber = rowNumber;
        }

        public int? RowNumber { get; }
    }
}
