namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IArquivoStorageService
    {
        Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
