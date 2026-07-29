using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Services
{
    public class ArquivoStorageService : IArquivoStorageService
    {
        private readonly string _baseDirectory;
        private readonly IMessageService _messageService;

        public ArquivoStorageService(IHostEnvironment environment, IOptions<StorageOptions> options, IMessageService messageService)
        {
            _messageService = messageService;
            var relativeDirectory = options.Value.UploadDirectory;
            _baseDirectory = Path.Combine(environment.ContentRootPath, relativeDirectory);
            Directory.CreateDirectory(_baseDirectory);
        }

        public async Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
        {
            var safeName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_baseDirectory, safeName);
            await File.WriteAllBytesAsync(filePath, content, cancellationToken);
            return filePath;
        }

        public Task<Stream> OpenReadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            Stream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = Path.GetFullPath(filePath);
            var baseDirectoryWithSeparator = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(_baseDirectory)) + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(baseDirectoryWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.InvalidOutputDirectory));
            }

            File.Delete(fullPath);
            return Task.CompletedTask;
        }
    }
}
