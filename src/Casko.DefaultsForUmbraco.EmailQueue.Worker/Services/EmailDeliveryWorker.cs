using System.Text.Json;
using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Contracts;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.ServiceBus;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.Smtp;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Services;

public sealed class EmailDeliveryWorker(
    ServiceBusClient serviceBusClient,
    IEmailPayloadReader payloadReader,
    IDeliveryReceiptStore receiptStore,
    IEmailSender emailSender,
    IOptions<EmailQueueOptions> queueOptions,
    IOptions<EmailWorkerOptions> workerOptions,
    ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> emailLocks = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = workerOptions.Value;
        await using var receiver = serviceBusClient.CreateReceiver(queueOptions.Value.QueueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await receiver.ReceiveMessagesAsync(
                settings.BatchSize,
                TimeSpan.FromSeconds(settings.ReceiveWaitSeconds),
                stoppingToken);

            using var concurrency = new SemaphoreSlim(settings.MaxConcurrency);
            var deliveries = messages.Select(async message =>
            {
                await concurrency.WaitAsync(stoppingToken);
                try
                {
                    await ProcessAsync(receiver, message, stoppingToken);
                }
                finally
                {
                    concurrency.Release();
                }
            });

            await Task.WhenAll(deliveries);
        }
    }

    private async Task ProcessAsync(ServiceBusReceiver receiver, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var requested = message.Body.ToObjectFromJson<EmailRequested>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (requested is null || requested.EmailId == Guid.Empty || string.IsNullOrWhiteSpace(requested.BlobName))
            {
                throw new EmailMessageFormatException("Message does not contain a valid email request.");
            }

            var emailLock = emailLocks.GetOrAdd(requested.EmailId, _ => new SemaphoreSlim(1, 1));
            await emailLock.WaitAsync(cancellationToken);
            try
            {
                if (await receiptStore.ExistsAsync(requested.EmailId, cancellationToken))
                {
                    logger.LogInformation("Skipping already delivered email {EmailId}", requested.EmailId);
                    await receiver.CompleteMessageAsync(message, cancellationToken);
                    return;
                }

                var payload = await payloadReader.ReadAsync(requested.BlobName, cancellationToken);
                if (payload.EmailId != requested.EmailId)
                {
                    throw new EmailMessageFormatException("Message email ID does not match its payload.");
                }

                await emailSender.SendAsync(payload, cancellationToken);
                var createdReceipt = await receiptStore.TryCreateAsync(payload.EmailId, cancellationToken);
                logger.LogInformation("Delivered email {EmailId} to {EmailRecipient}; receipt created: {ReceiptCreated}", payload.EmailId, payload.To, createdReceipt);
                await receiver.CompleteMessageAsync(message, cancellationToken);
            }
            finally
            {
                emailLock.Release();
            }
        }
        catch (Exception exception) when (IsPermanent(exception))
        {
            logger.LogWarning(exception, "Dead-lettering email message {MessageId}", message.MessageId);
            await receiver.DeadLetterMessageAsync(message, "PermanentDeliveryFailure", exception.Message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Abandoning email message {MessageId} for retry", message.MessageId);
            await receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
        }
    }

    private static bool IsPermanent(Exception exception) => exception is EmailMessageFormatException
        or EmailPayloadNotFoundException
        || exception is SmtpCommandException smtpException && (int)smtpException.StatusCode >= 500;
}
