using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.Smtp;

public sealed class MailKitEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailPayload payload, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(payload.To));
        message.Subject = payload.Subject;
        message.Body = new TextPart(TextFormat.Plain) { Text = payload.Body };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
