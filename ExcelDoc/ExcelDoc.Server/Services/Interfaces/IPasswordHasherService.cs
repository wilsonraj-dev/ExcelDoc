namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IPasswordHasherService
    {
        string Hash(string password);

        bool Verify(string password, string passwordHash);

        bool NeedsRehash(string passwordHash);
    }
}
