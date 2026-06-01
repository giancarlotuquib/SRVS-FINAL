namespace SRVS.Application.Abstractions;

public interface ISyllabusFileStorage
{
    Task<string> SaveAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}