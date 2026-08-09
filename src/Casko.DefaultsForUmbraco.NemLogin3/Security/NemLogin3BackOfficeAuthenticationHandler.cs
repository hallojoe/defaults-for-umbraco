using System.Security.Claims;
using System.Text.Encodings.Web;
using Casko.Authentication.NemLogin3.Web.Configuration;
using Casko.Authentication.NemLogin3.Web.Services;
using Casko.DefaultsForUmbraco.NemLogin3.Configuration;
using Casko.DefaultsForUmbraco.NemLogin3.Services;
using ITfoxtec.Identity.Saml2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.NemLogin3.Security;

public sealed class NemLogin3BackOfficeAuthenticationHandler(
    IOptionsMonitor<NemLogin3AuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDataProtectionProvider dataProtectionProvider,
    Saml2Configuration saml2Configuration,
    IOptions<NemLogin3Options> nemLoginOptions,
    INemLogin3ClaimsTransformer claimsTransformer,
    INemLogin3BackOfficeClaimsMapper backOfficeClaimsMapper,
    IDistributedCache distributedCache)
    : NemLogin3AuthenticationHandlerBase(
        options,
        logger,
        encoder,
        dataProtectionProvider,
        saml2Configuration,
        nemLoginOptions,
        claimsTransformer,
        distributedCache)
{
    private readonly INemLogin3BackOfficeClaimsMapper _backOfficeClaimsMapper = backOfficeClaimsMapper;

    protected override ClaimsPrincipal MapClaims(ClaimsPrincipal principal)
        => _backOfficeClaimsMapper.Map(principal);

    protected override void PrepareChallengeProperties(AuthenticationProperties properties)
    {
        if (!string.IsNullOrWhiteSpace(properties.RedirectUri) && properties.RedirectUri != "/")
        {
            return;
        }

        properties.RedirectUri = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
    }
}
