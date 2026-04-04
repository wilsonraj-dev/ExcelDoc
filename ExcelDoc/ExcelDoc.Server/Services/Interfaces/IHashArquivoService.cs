namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IHashArquivoService
    {
        string ComputeSha256(byte[] content);
    }
}
