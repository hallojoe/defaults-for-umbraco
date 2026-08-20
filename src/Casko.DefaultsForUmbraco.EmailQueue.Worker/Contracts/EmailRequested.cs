namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Contracts;

public sealed record EmailRequested(Guid EmailId, string BlobName);
