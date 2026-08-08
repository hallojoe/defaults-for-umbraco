using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Http;

namespace Casko.NemLogin3.Web.Services;

/// <summary>
/// Creates service provider metadata for NemLog-in.
/// </summary>
public interface INemLogin3MetadataService
{
    /// <summary>
    /// Creates the SAML service provider entity descriptor for the current request.
    /// </summary>
    EntityDescriptor CreateMetadata(HttpRequest request);
}
