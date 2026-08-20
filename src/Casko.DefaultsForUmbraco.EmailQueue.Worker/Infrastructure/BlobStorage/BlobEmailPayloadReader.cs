using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;

public sealed class BlobEmailPayloadReader(BlobServiceClient blobServiceClient, IOptions<EmailQueueOptions> options) : IEmailPayloadReader
{
    public async Task<EmailPayload> ReadAsync(string blobName, CancellationToken cancellationToken)
    {
        var blob = blobServiceClient
            .GetBlobContainerClient(options.Value.BlobContainerName)
            .GetBlobClient(blobName);

        try
        {
            var content = await blob.DownloadContentAsync(cancellationToken);
            return content.Value.Content.ToObjectFromJson<EmailPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException($"Email payload blob '{blobName}' is empty.");
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            throw new EmailPayloadNotFoundException(blobName);
        }
    }
}
