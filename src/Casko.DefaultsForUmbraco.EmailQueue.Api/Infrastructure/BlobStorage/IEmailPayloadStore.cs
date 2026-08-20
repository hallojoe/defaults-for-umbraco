using Casko.DefaultsForUmbraco.EmailQueue.Api.Models;

namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.BlobStorage;

public interface IEmailPayloadStore
{
    Task<string> StoreAsync(EmailPayload payload, CancellationToken cancellationToken);
}
