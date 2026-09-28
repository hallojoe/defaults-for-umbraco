using Umbraco.Automate.Core.Settings;

namespace Casko.DefaultsForUmbraco.Automate.Actions.SendEmail;

public sealed class SendEmailSettings
{
    [Field(
        Label = "Content key",
        Description = "Bind this to the Content Published trigger's {{ trigger.contentKey }} value.",
        SupportsBindings = true)]
    public string ContentKey { get; set; } = string.Empty;
}
