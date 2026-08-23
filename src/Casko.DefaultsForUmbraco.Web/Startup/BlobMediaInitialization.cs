using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;

namespace Casko.DefaultsForUmbraco.Web.Startup;

internal static class BlobMediaInitialization
{
    public static async Task InitializeBlobMediaContainerAsync(this WebApplication application, string? blobsConnectionString)
    {
        if (string.IsNullOrWhiteSpace(blobsConnectionString))
        {
            return;
        }

        var mediaContainerName = application.Configuration["Umbraco:Storage:AzureBlob:Media:ContainerName"];
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaContainerName);

        var blobServiceClient = new BlobServiceClient(blobsConnectionString);
        await blobServiceClient
            .GetBlobContainerClient(mediaContainerName)
            .CreateIfNotExistsAsync();
    }
}
