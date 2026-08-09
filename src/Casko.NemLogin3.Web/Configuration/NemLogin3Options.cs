namespace Casko.NemLogin3.Web.Configuration;

public class NemLogin3Options
{
    public const string SectionName = "NemLogin3";

    public string? PublicBaseUrl { get; set; }

    public string MetadataPath { get; set; } = "/Metadata";

    public string LoginPath { get; set; } = "/Auth/Login";

    public string AssertionConsumerServicePath { get; set; } = "/Auth/AssertionConsumerService";

    public List<string> AdditionalAssertionConsumerServicePaths { get; set; } = [];

    public string SingleLogoutPath { get; set; } = "/Auth/SingleLogout";

    public string LoggedOutPath { get; set; } = "/Auth/LoggedOut";

    public string RequestedAuthnContext { get; set; } = "https://data.gov.dk/concept/core/nsis/loa/Substantial";

    public string RequestedAuthnContextComparison { get; set; } = "Minimum";

    public bool UseForwardedHeaders { get; set; } = true;

    public MetadataOptions Metadata { get; set; } = new();
}

public class MetadataOptions
{
    public string ServiceName { get; set; } = "Casko NemLogin3 Demo";

    public OrganizationOptions Organization { get; set; } = new();

    public ContactOptions Contact { get; set; } = new();

    public List<RequestedAttributeOptions> RequestedAttributes { get; set; } = [];
}

public class OrganizationOptions
{
    public string Name { get; set; } = "Casko";

    public string DisplayName { get; set; } = "Casko";

    public string Url { get; set; } = "https://samlcasko0001.dev.localhost";
}

public class ContactOptions
{
    public string Company { get; set; } = "Casko";

    public string GivenName { get; set; } = "Demo";

    public string SurName { get; set; } = "Administrator";

    public string EmailAddress { get; set; } = "support@example.local";

    public string TelephoneNumber { get; set; } = "00000000";
}

public class RequestedAttributeOptions
{
    public string Name { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public string NameFormat { get; set; } = NemLogin3ClaimConstants.AttributeNameFormatUri;
}
