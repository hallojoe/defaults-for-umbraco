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
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
        {
            ModelState.AddModelError(nameof(request), "Subject and body must not be empty.");
            return ValidationProblem(ModelState);
        }

        var emailId = Guid.NewGuid();
        var payload = new EmailPayload(emailId, request.To, request.Subject, request.Body);
        var blobName = await payloadStore.StoreAsync(payload, cancellationToken);

        await publisher.PublishAsync(new EmailRequested(emailId, blobName), cancellationToken);
        logger.LogInformation("Queued email {EmailId} for {EmailRecipient}", emailId, request.To);

        return Accepted(new { emailId });
    }
}
