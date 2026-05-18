using Microsoft.Extensions.Hosting;
using SRVS.Application.Abstractions;

namespace SRVS.Infrastructure.Services;

public class LocalSyllabusFileStorage(IHostEnvironment hostEnvironment) : ISyllabusFileStorage
{
    private const string StorageFolderName = "syllabi";

    public async Task<string> SaveAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var storageRoot = GetStorageRoot();
        Directory.CreateDirectory(storageRoot);

        var safeFileName = Path.GetFileName(fileName);
        var storagePath = Path.Combine(storageRoot, safeFileName);

        await using var outputStream = File.Create(storagePath);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        return storagePath;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(storagePath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }

        return Task.CompletedTask;
    }

    private string GetStorageRoot()
    {
        return Path.Combine(hostEnvironment.ContentRootPath, "App_Data", StorageFolderName);
    }
}