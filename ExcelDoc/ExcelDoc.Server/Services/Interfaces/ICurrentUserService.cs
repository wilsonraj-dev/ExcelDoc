using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface ICurrentUserService
    {
        Usuario GetRequiredUser();
    }
}
