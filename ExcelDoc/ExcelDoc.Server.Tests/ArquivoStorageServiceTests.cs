using ExcelDoc.Server.Options;
using ExcelDoc.Server.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExcelDoc.Server.Tests;

public sealed class ArquivoStorageServiceTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(
        Path.GetTempPath(),
        $"exceldoc-storage-{Guid.NewGuid():N}");

    [Fact]
    public async Task DeleteAsync_RemovesSavedUpload()
    {
        var service = CreateService();
        var filePath = await service.SaveAsync("planilha.xlsx", [1, 2, 3]);

        await service.DeleteAsync(filePath);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteAsync_RejectsFileOutsideUploadDirectory()
    {
        var service = CreateService();
        var outsidePath = Path.Combine(_contentRoot, "outside.xlsx");
        Directory.CreateDirectory(_contentRoot);
        await File.WriteAllBytesAsync(outsidePath, [1, 2, 3]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAsync(outsidePath));

        Assert.True(File.Exists(outsidePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    private ArquivoStorageService CreateService()
    {
        var environment = new TestHostEnvironment
        {
            ContentRootPath = _contentRoot,
            ContentRootFileProvider = new NullFileProvider()
        };
        var options = Microsoft.Extensions.Options.Options.Create(new StorageOptions
        {
            UploadDirectory = "App_Data/Uploads"
        });

        return new ArquivoStorageService(environment, options);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ExcelDoc.Server.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
