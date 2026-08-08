namespace Casko.DefaultsForUmbraco.NemLogin3.Configuration;

public class NemLogin3MemberLoginOptions
{
    public const string SectionName = "NemLogin3:Members";

    public string SchemeName { get; set; } = NemLogin3MemberLoginConstants.SchemeName;

    public string DisplayName { get; set; } = "NemLog-in";

    public string SyntheticEmailDomain { get; set; } = "nemlogin.local";

    public string? CorrelationCookieDomain { get; set; }

    public bool AutoLinkExternalAccount { get; set; } = true;

    public bool DefaultIsApproved { get; set; } = true;

    public bool ExternalOnly { get; set; } = true;

    public string? DefaultCulture { get; set; } = "en";

    public string DefaultMemberTypeAlias { get; set; } = Umbraco.Cms.Core.Constants.Security.DefaultMemberTypeAlias;

    public List<string> DefaultMemberGroups { get; set; } = [];
}
