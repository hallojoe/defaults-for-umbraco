using Casko.DefaultsForUmbraco.EmailQueue.Api.Contracts;

namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.ServiceBus;

public interface IEmailRequestedPublisher
{
    Task PublishAsync(EmailRequested emailRequested, CancellationToken cancellationToken);
}
