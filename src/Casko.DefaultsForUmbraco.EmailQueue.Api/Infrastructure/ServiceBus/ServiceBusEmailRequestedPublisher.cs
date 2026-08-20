using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Contracts;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Models;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.ServiceBus;

public sealed class ServiceBusEmailRequestedPublisher(ServiceBusClient client, IOptions<EmailQueueOptions> options) : IEmailRequestedPublisher
{
    public async Task PublishAsync(EmailRequested emailRequested, CancellationToken cancellationToken)
    {
        await using var sender = client.CreateSender(options.Value.QueueName);
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(emailRequested, new JsonSerializerOptions(JsonSerializerDefaults.Web)))
        {
            MessageId = emailRequested.EmailId.ToString("N"),
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);
    }
}
