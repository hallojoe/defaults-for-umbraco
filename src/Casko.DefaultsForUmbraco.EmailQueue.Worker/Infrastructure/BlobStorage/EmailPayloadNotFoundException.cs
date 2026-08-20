namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;

public sealed class EmailPayloadNotFoundException(string blobName) : Exception($"Email payload blob '{blobName}' was not found.");
