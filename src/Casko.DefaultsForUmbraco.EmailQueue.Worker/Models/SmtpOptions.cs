using System.ComponentModel.DataAnnotations;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required, EmailAddress]
    public string From { get; init; } = "noreply@example.local";

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1025;

    public bool UseStartTls { get; init; }
}
