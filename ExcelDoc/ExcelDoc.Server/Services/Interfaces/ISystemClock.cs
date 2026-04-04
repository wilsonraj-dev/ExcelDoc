namespace ExcelDoc.Server.Services.Interfaces
{
    public interface ISystemClock
    {
        DateTime UtcNow { get; }
    }
}
