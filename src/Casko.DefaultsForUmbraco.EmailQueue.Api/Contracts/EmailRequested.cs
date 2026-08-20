namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Contracts;

public sealed record EmailRequested(Guid EmailId, string BlobName);
