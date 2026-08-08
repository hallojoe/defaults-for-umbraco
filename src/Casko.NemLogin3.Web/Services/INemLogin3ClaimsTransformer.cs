using System.Security.Claims;

namespace Casko.NemLogin3.Web.Services;

/// <summary>
/// Transforms claims from a validated NemLog-in SAML response before the local session is created.
/// </summary>
public interface INemLogin3ClaimsTransformer
{
    /// <summary>
    /// Transforms the incoming principal into the principal stored in the application session.
    /// </summary>
    ClaimsPrincipal Transform(ClaimsPrincipal incomingPrincipal);
}
