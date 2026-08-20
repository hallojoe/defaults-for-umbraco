namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Models;

public sealed record EmailPayload(Guid EmailId, string To, string Subject, string Body);
