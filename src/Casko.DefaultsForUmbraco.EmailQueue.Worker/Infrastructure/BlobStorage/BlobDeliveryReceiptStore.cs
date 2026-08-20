using Azure;
using Azure.Storage.Blobs;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;

public sealed class BlobDeliveryReceiptStore(BlobServiceClient blobServiceClient, IOptions<EmailQueueOptions> options) : IDeliveryReceiptStore
{
    public async Task<bool> ExistsAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var response = await GetReceiptBlob(emailId).ExistsAsync(cancellationToken);
        return response.Value;
    }

    public async Task<bool> TryCreateAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var container = blobServiceClient.GetBlobContainerClient(options.Value.BlobContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        try
        {
            await GetReceiptBlob(emailId).UploadAsync(
                BinaryData.FromString($"{{\"emailId\":\"{emailId:D}\",\"deliveredAtUtc\":\"{DateTimeOffset.UtcNow:O}\"}}"),
                overwrite: false,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            return false;
        }
    }

    private BlobClient GetReceiptBlob(Guid emailId) => blobServiceClient
        .GetBlobContainerClient(options.Value.BlobContainerName)
        .GetBlobClient($"receipts/{emailId:N}.json");
}
