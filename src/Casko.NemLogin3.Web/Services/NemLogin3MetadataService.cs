using Casko.NemLogin3.Web.Configuration;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Casko.NemLogin3.Web.Services;

public class NemLogin3MetadataService(
    Saml2Configuration saml2Configuration,
    IOptions<NemLogin3Options> options)
    : INemLogin3MetadataService
{
    private readonly NemLogin3Options _options = options.Value;

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
                        Location = new Uri(defaultSite, _options.SingleLogoutPath.TrimStart('/')),
                        ResponseLocation = new Uri(defaultSite, _options.LoggedOutPath.TrimStart('/'))
                    }
                ],
                NameIDFormats = [NameIdentifierFormats.Persistent],
                AssertionConsumerServices = CreateAssertionConsumerServices(defaultSite),
                AttributeConsumingServices =
                [
                    new AttributeConsumingService
                    {
                        ServiceNames = [new LocalizedNameType(_options.Metadata.ServiceName, "en")],
                        RequestedAttributes = CreateRequestedAttributes()
                    }
                ],
            }
        };

        entityDescriptor.SPSsoDescriptor.SetDefaultEncryptionMethods();
        entityDescriptor.Organization = new Organization(
            [new LocalizedNameType(_options.Metadata.Organization.Name, "en")],
            [new LocalizedNameType(_options.Metadata.Organization.DisplayName, "en")],
            [new LocalizedUriType(new Uri(_options.Metadata.Organization.Url), "en")]);
        entityDescriptor.ContactPersons =
        [
            new ContactPerson(ContactTypes.Technical)
            {
                Company = _options.Metadata.Contact.Company,
                GivenName = _options.Metadata.Contact.GivenName,
                SurName = _options.Metadata.Contact.SurName,
                EmailAddress = _options.Metadata.Contact.EmailAddress,
                TelephoneNumber = _options.Metadata.Contact.TelephoneNumber,
            }
        ];

        return entityDescriptor;
    }

    private IEnumerable<RequestedAttribute> CreateRequestedAttributes()
    {
        var requestedAttributes = _options.Metadata.RequestedAttributes.Count > 0
            ? _options.Metadata.RequestedAttributes
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

    private List<AssertionConsumerService> CreateAssertionConsumerServices(Uri defaultSite)
    {
        var paths = new[] { _options.AssertionConsumerServicePath }
            .Concat(_options.AdditionalAssertionConsumerServicePaths)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return paths
            .Select((path, index) => new AssertionConsumerService
            {
                Binding = ProtocolBindings.HttpPost,
                Location = new Uri(defaultSite, path.TrimStart('/')),
                Index = index,
                IsDefault = index == 0,
            })
            .ToList();
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
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return new Uri(EnsureTrailingSlash(_options.PublicBaseUrl));
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
