# Blob storage runs locally too

Azurite gives us an Azure Blob Storage-compatible service with a persistent volume.

```csharp
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(x => x.WithDataVolume("defaults-for-umbraco-azurite-data"));
var blobs = storage.AddBlobs("blobs");
```
