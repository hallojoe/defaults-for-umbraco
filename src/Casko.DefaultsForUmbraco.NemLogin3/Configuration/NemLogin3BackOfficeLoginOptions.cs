namespace Casko.DefaultsForUmbraco.NemLogin3.Configuration;

public class NemLogin3BackOfficeLoginOptions
{
    public const string SectionName = "NemLogin3:BackOffice";

    public string SchemeName { get; set; } = NemLogin3BackOfficeLoginConstants.SchemeName;

    public string DisplayName { get; set; } = "NemLog-in";

    public string CallbackPath { get; set; } = "/Auth/AssertionConsumerService";

    public string? CorrelationCookieDomain { get; set; }

    public string SyntheticEmailDomain { get; set; } = "nemlogin.local";

    public bool AutoLinkExternalAccount { get; set; }

    public bool DefaultIsApproved { get; set; }

    public bool AllowManualLinking { get; set; } = true;

    public string? DefaultCulture { get; set; }

    public List<string> DefaultUserGroups { get; set; } = [];
}
