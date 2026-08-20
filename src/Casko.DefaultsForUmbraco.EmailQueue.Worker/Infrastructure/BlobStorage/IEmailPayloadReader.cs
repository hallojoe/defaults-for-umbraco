using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;

public interface IEmailPayloadReader
{
    Task<EmailPayload> ReadAsync(string blobName, CancellationToken cancellationToken);
}
