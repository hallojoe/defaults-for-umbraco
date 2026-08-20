using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.Smtp;

public interface IEmailSender
{
    Task SendAsync(EmailPayload payload, CancellationToken cancellationToken);
}
