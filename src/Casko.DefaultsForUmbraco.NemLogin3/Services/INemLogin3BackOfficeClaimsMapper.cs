using System.Security.Claims;

namespace Casko.DefaultsForUmbraco.NemLogin3.Services;

public interface INemLogin3BackOfficeClaimsMapper
{
    ClaimsPrincipal Map(ClaimsPrincipal principal);
}
