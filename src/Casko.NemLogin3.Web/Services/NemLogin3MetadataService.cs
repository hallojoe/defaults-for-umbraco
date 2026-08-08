using Casko.NemLogin3.Web.Configuration;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Casko.NemLogin3.Web.Services;

public class NemLogin3MetadataService : INemLogin3MetadataService
{
    private readonly Saml2Configuration saml2Configuration;
    private readonly NemLogin3Options options;

    public NemLogin3MetadataService(
        Saml2Configuration saml2Configuration,
        IOptions<NemLogin3Options> options)
    {
        this.saml2Configuration = saml2Configuration;
        this.options = options.Value;
    }

    /// <inheritdoc />
    public EntityDescriptor CreateMetadata(HttpRequest request)
    {
        var defaultSite = GetPublicBaseUri(request);
        var entityDescriptor = new NemLoginEntityDescriptor(saml2Configuration)
        {
            ValidUntil = 365,
            SPSsoDescriptor = new SPSsoDescriptor
            {
                CertificateIncludeOption = X509IncludeOption.EndCertOnly,
                AuthnRequestsSigned = true,
                WantAssertionsSigned = true,
                SigningCertificates =
                [
                    saml2Configuration.SigningCertificate
                ],
                EncryptionCertificates = saml2Configuration.DecryptionCertificates,
                SingleLogoutServices =
                [
                    new SingleLogoutService
                    {
                        Binding = ProtocolBindings.HttpPost,
                        Location = new Uri(defaultSite, options.SingleLogoutPath.TrimStart('/')),
                        ResponseLocation = new Uri(defaultSite, options.LoggedOutPath.TrimStart('/'))
                    }
                ],
                NameIDFormats = [NameIdentifierFormats.Persistent],
                AssertionConsumerServices =
                [
                    new AssertionConsumerService
                    {
                        Binding = ProtocolBindings.HttpPost,
                        Location = new Uri(defaultSite, options.AssertionConsumerServicePath.TrimStart('/'))
                    },
                ],
                AttributeConsumingServices =
                [
                    new AttributeConsumingService
                    {
                        ServiceNames = [new LocalizedNameType(options.Metadata.ServiceName, "en")],
                        RequestedAttributes = CreateRequestedAttributes()
                    }
                ],
            }
        };

        entityDescriptor.SPSsoDescriptor.SetDefaultEncryptionMethods();
        entityDescriptor.Organization = new Organization(
            [new LocalizedNameType(options.Metadata.Organization.Name, "en")],
            [new LocalizedNameType(options.Metadata.Organization.DisplayName, "en")],
            [new LocalizedUriType(new Uri(options.Metadata.Organization.Url), "en")]);
        entityDescriptor.ContactPersons =
        [
            new ContactPerson(ContactTypes.Technical)
            {
                Company = options.Metadata.Contact.Company,
                GivenName = options.Metadata.Contact.GivenName,
                SurName = options.Metadata.Contact.SurName,
                EmailAddress = options.Metadata.Contact.EmailAddress,
                TelephoneNumber = options.Metadata.Contact.TelephoneNumber,
            }
        ];

        return entityDescriptor;
    }

    private IEnumerable<RequestedAttribute> CreateRequestedAttributes()
    {
        var requestedAttributes = options.Metadata.RequestedAttributes.Count > 0
            ? options.Metadata.RequestedAttributes
            : CreateDefaultPrivateProfileAttributes();

        return requestedAttributes
            .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name))
            .Select(attribute => new RequestedAttribute(
                attribute.Name,
                nameFormat: string.IsNullOrWhiteSpace(attribute.NameFormat)
                    ? NemLogin3ClaimConstants.AttributeNameFormatUri
                    : attribute.NameFormat,
                isRequired: attribute.IsRequired));
    }

    private static List<RequestedAttributeOptions> CreateDefaultPrivateProfileAttributes()
    {
        return
        [
            new() { Name = NemLogin3ClaimConstants.SpecVersion },
            new() { Name = NemLogin3ClaimConstants.NsisLoa },
            new() { Name = NemLogin3ClaimConstants.CprUuid },
            new() { Name = NemLogin3ClaimConstants.FullName },
            new() { Name = NemLogin3ClaimConstants.ProfessionalCvr, IsRequired = false },
            new() { Name = NemLogin3ClaimConstants.ProfessionalOrgName, IsRequired = false },
        ];
    }

    private Uri GetPublicBaseUri(HttpRequest request)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return new Uri(EnsureTrailingSlash(options.PublicBaseUrl));
        }

        return new Uri($"{request.Scheme}://{request.Host.ToUriComponent()}/");
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
    }

    private sealed class NemLoginEntityDescriptor : EntityDescriptor
    {
        public NemLoginEntityDescriptor(Saml2Configuration config)
            : base(config)
        {
            MetadataSigningCertificate = null;
        }
    }
}
