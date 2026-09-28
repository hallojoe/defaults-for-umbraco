using Microsoft.Extensions.Logging;

namespace Casko.DefaultsForUmbraco.Automate.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Email delivery is not configured. Would send subject {Subject} to list aliases {ListAliases}.",
            message.Subject,
            string.Join(", ", message.ListAliases));

        return Task.CompletedTask;
    }
}
