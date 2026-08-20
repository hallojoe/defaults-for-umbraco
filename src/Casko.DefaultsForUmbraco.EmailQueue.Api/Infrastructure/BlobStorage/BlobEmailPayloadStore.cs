using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Models;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.BlobStorage;

public sealed class BlobEmailPayloadStore(BlobServiceClient blobServiceClient, IOptions<EmailQueueOptions> options) : IEmailPayloadStore
{
    public async Task<string> StoreAsync(EmailPayload payload, CancellationToken cancellationToken)
    {
        var container = blobServiceClient.GetBlobContainerClient(options.Value.BlobContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"payloads/{payload.EmailId:N}.json";
        var blob = container.GetBlobClient(blobName);
        var json = BinaryData.FromObjectAsJson(payload);
        await blob.UploadAsync(json, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
        }, cancellationToken);

        return blobName;
    }
}
