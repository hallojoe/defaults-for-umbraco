using System.Security.Claims;
using Casko.DefaultsForUmbraco.NemLogin3.Configuration;
using Casko.NemLogin3.Web.Configuration;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.NemLogin3.Services;

public sealed class NemLogin3BackOfficeClaimsMapper(IOptions<NemLogin3BackOfficeLoginOptions> options)
    : INemLogin3BackOfficeClaimsMapper
{
    private readonly NemLogin3BackOfficeLoginOptions _options = options.Value;

    public ClaimsPrincipal Map(ClaimsPrincipal principal)
    {
        var cprUuid = principal.FindFirstValue(NemLogin3ClaimConstants.CprUuid);
        if (string.IsNullOrWhiteSpace(cprUuid))
        {
            throw new InvalidOperationException($"NemLog-in claim '{NemLogin3ClaimConstants.CprUuid}' is required to sign in an Umbraco backoffice user.");
        }

        var fullName = principal.FindFirstValue(NemLogin3ClaimConstants.FullName);
        var email = CreateSyntheticEmail(cprUuid);
        var claims = principal.Claims.ToList();
        AddOrReplace(claims, ClaimTypes.NameIdentifier, cprUuid);
        AddOrReplace(claims, ClaimTypes.Email, email);
        AddOrReplace(claims, ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? cprUuid : fullName);
        AddOrReplace(claims, ClaimTypes.Upn, email);

        var authenticationType = principal.Identity?.AuthenticationType ?? NemLogin3BackOfficeLoginConstants.SchemeName;
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType, ClaimTypes.Name, ClaimTypes.Role));
    }

    private string CreateSyntheticEmail(string cprUuid)
    {
        var normalized = new string(cprUuid
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.' ? character : '-')
            .ToArray());

        return $"{normalized}@{_options.SyntheticEmailDomain}";
    }

    private static void AddOrReplace(List<Claim> claims, string type, string value)
    {
        claims.RemoveAll(claim => claim.Type == type);
        claims.Add(new Claim(type, value));
    }
}
