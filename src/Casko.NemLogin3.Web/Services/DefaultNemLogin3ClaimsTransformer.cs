using System.Security.Claims;

namespace Casko.NemLogin3.Web.Services;

public class DefaultNemLogin3ClaimsTransformer : INemLogin3ClaimsTransformer
{
    /// <inheritdoc />
    public ClaimsPrincipal Transform(ClaimsPrincipal incomingPrincipal)
    {
        if (incomingPrincipal.Identity is { IsAuthenticated: false })
        {
            return incomingPrincipal;
        }

        var claims = incomingPrincipal.Claims.ToList();
        var incomingIdentity = (ClaimsIdentity)incomingPrincipal.Identity!;

        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            incomingIdentity.AuthenticationType,
            ClaimTypes.NameIdentifier,
            ClaimTypes.Role)
        {
            BootstrapContext = incomingIdentity.BootstrapContext
        });
    }
}
