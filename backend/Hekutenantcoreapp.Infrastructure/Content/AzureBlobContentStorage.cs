using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Hekutenantcoreapp.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Hekutenantcoreapp.Infrastructure.Content;

// Connection-string auth (see the design doc — managed identity deferred). The target container
// is assumed to already exist (created once via the portal/az CLI, private access) — this class
// deliberately doesn't auto-create it on every resolve.
public class AzureBlobContentStorage : IContentStorage
{
    private readonly BlobContainerClient _container;
    private readonly IContentPartitionResolver _partitionResolver;

    public AzureBlobContentStorage(IConfiguration configuration, IContentPartitionResolver partitionResolver)
    {
        var connectionString = configuration["ContentStorage:AzureBlob:ConnectionString"];
        var containerName = configuration["ContentStorage:AzureBlob:Container"] ?? "content";
        var serviceClient = new BlobServiceClient(connectionString);
        _container = serviceClient.GetBlobContainerClient(containerName);
        _partitionResolver = partitionResolver;
    }

    public async Task<StoredBlobRef> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var partition = await _partitionResolver.ResolveAsync(ct);
        var ext = Path.GetExtension(fileName);
        var blobName = $"{partition}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);

        return new StoredBlobRef(_container.Name, blobName);
    }

    public async Task<Stream> OpenReadAsync(string container, string blobName, CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobName);
        var download = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return download.Value.Content;
    }

    public async Task DeleteAsync(string container, string blobName, CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
