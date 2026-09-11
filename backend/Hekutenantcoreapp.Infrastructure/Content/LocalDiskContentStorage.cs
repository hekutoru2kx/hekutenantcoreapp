using Hekutenantcoreapp.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Hekutenantcoreapp.Infrastructure.Content;

// Dev/offline fallback — keeps the app buildable and runnable with no Azure account, mirroring
// how the DB connection string ships blank. Never used in production (ContentStorage:Provider
// defaults to "LocalDisk" precisely so a fresh clone works before anyone touches Azure).
public class LocalDiskContentStorage : IContentStorage
{
    private readonly string _root;
    private readonly IContentPartitionResolver _partitionResolver;

    public LocalDiskContentStorage(IConfiguration configuration, IContentPartitionResolver partitionResolver)
    {
        var configuredRoot = configuration["ContentStorage:LocalDisk:RootPath"];
        _root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads")
            : configuredRoot;
        _partitionResolver = partitionResolver;
    }

    public async Task<StoredBlobRef> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var partition = await _partitionResolver.ResolveAsync(ct);
        var blobName = BuildBlobName(partition, fileName);
        var fullPath = ToFullPath(blobName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return new StoredBlobRef("local", blobName);
    }

    public Task<Stream> OpenReadAsync(string container, string blobName, CancellationToken ct = default) =>
        Task.FromResult<Stream>(File.OpenRead(ToFullPath(blobName)));

    public Task DeleteAsync(string container, string blobName, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(blobName);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private static string BuildBlobName(string partition, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return $"{partition}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";
    }

    private string ToFullPath(string blobName) =>
        Path.Combine(_root, blobName.Replace('/', Path.DirectorySeparatorChar));
}
