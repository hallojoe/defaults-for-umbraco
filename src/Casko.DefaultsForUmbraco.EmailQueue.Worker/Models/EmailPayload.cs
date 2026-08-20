namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;

public sealed record EmailPayload(Guid EmailId, string To, string Subject, string Body);
