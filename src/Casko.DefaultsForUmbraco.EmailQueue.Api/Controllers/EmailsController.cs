using Casko.DefaultsForUmbraco.EmailQueue.Api.Contracts;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.BlobStorage;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.ServiceBus;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Controllers;

[ApiController]
[Route("emails")]
public sealed class EmailsController(
    IEmailPayloadStore payloadStore,
    IEmailRequestedPublisher publisher,
    ILogger<EmailsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(SubmitEmailRequest request, CancellationToken cancellationToken)
    {
        if (!IsValid(request))
        {
            ModelState.AddModelError(nameof(request), "Subject and body must not be empty.");
            return ValidationProblem(ModelState);
        }

        var emailId = await QueueAsync(request.To, request.Subject, request.Body, cancellationToken);

        return Accepted(new { emailId });
    }

    /// <summary>
    /// Queues multiple numbered copies of an email for local throughput testing.
    /// </summary>
    [HttpPost("batch/{count:int}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostBatch([FromRoute] int count, SubmitEmailRequest request, CancellationToken cancellationToken)
    {
        if (count is < 1 or > 1000)
        {
            ModelState.AddModelError(nameof(count), "Count must be between 1 and 1000.");
        }

        if (!IsValid(request))
        {
            ModelState.AddModelError(nameof(request), "Subject and body must not be empty.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var emailIds = new List<Guid>(count);
        for (var number = 1; number <= count; number++)
        {
            emailIds.Add(await QueueAsync(
                request.To,
                $"{request.Subject} {number}",
                $"{request.Body} {number}",
                cancellationToken));
        }

        return Accepted(new { count, emailIds });
    }

    private async Task<Guid> QueueAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var emailId = Guid.NewGuid();
        var payload = new EmailPayload(emailId, recipient, subject, body);
        var blobName = await payloadStore.StoreAsync(payload, cancellationToken);

        await publisher.PublishAsync(new EmailRequested(emailId, blobName), cancellationToken);
        logger.LogInformation("Queued email {EmailId} for {EmailRecipient}", emailId, recipient);
        return emailId;
    }

    private static bool IsValid(SubmitEmailRequest request) =>
        !string.IsNullOrWhiteSpace(request.Subject) && !string.IsNullOrWhiteSpace(request.Body);
}
