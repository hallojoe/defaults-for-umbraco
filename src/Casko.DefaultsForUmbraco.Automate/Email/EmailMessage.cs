namespace Casko.DefaultsForUmbraco.Automate.Email;

public sealed record EmailMessage(
    IReadOnlyCollection<string> ListAliases,
    string Subject,
    string HtmlBody);
