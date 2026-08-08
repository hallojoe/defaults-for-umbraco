using System.Security.Claims;

namespace Casko.DefaultsForUmbraco.NemLogin3.Services;

public interface INemLogin3MemberClaimsMapper
{
    ClaimsPrincipal Map(ClaimsPrincipal principal);
}
