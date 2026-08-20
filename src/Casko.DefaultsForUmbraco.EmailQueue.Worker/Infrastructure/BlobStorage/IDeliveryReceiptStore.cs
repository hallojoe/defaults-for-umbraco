namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;

public interface IDeliveryReceiptStore
{
    Task<bool> ExistsAsync(Guid emailId, CancellationToken cancellationToken);

    Task<bool> TryCreateAsync(Guid emailId, CancellationToken cancellationToken);
}
