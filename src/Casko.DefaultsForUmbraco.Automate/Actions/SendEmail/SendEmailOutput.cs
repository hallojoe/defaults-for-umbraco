namespace Casko.DefaultsForUmbraco.Automate.Actions.SendEmail;

public sealed class SendEmailOutput
{
    public bool Sent { get; init; }

    public string? Reason { get; init; }

    public IReadOnlyCollection<string> ListAliases { get; init; } = Array.Empty<string>();
}
