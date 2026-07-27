namespace ExcelDoc.Server.Sap;

public sealed class SapSessionExpiredException : UnauthorizedAccessException
{
    public SapSessionExpiredException(string message)
        : base(message)
    {
    }

    public SapSessionExpiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
