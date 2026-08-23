using Aspire.Hosting.Azure;

namespace Casko.DefaultsForUmbraco.Aspire.AppHost;

internal static class StorageResourceExtensions
{
    public static StorageResources AddStorageResources(this IDistributedApplicationBuilder builder)
    {
        var storage = builder
            .AddAzureStorage("storage")
            .RunAsEmulator(emulator => emulator.WithDataVolume("defaults-for-umbraco-azurite-data"));

        return new StorageResources(storage, storage.AddBlobs("blobs"));
    }
}

internal sealed record StorageResources(
    IResourceBuilder<AzureStorageResource> Storage,
    IResourceBuilder<AzureBlobStorageResource> Blobs);