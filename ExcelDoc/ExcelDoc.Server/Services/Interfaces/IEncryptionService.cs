namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IEncryptionService
    {
        string Encrypt(string value);

        string Decrypt(string value);
    }
}
