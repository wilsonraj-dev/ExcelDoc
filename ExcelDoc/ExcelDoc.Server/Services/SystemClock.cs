using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class SystemClock : ISystemClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
