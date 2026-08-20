using System.ComponentModel.DataAnnotations;

namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Contracts;

public sealed class SubmitEmailRequest
{
    [Required, EmailAddress]
    public string To { get; init; } = string.Empty;

    [Required]
    public string Subject { get; init; } = string.Empty;

    [Required]
    public string Body { get; init; } = string.Empty;
}
