using System.Security.Claims;
using System.Text.Json;
using Casko.DefaultsForUmbraco.NemLogin3.Configuration;
using Casko.DefaultsForUmbraco.NemLogin3.Models;
using Casko.NemLogin3.Web.Configuration;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.NemLogin3.Services;

public sealed class NemLogin3MemberClaimsMapper(IOptions<NemLogin3MemberLoginOptions> options)
    : INemLogin3MemberClaimsMapper
{
    private readonly NemLogin3MemberLoginOptions _options = options.Value;

    public ClaimsPrincipal Map(ClaimsPrincipal principal)
    {
        var cprUuid = principal.FindFirstValue(NemLogin3ClaimConstants.CprUuid);
        if (string.IsNullOrWhiteSpace(cprUuid))
        {
            throw new InvalidOperationException($"NemLog-in claim '{NemLogin3ClaimConstants.CprUuid}' is required to sign in an Umbraco member.");
        }

        var fullName = principal.FindFirstValue(NemLogin3ClaimConstants.FullName);
        var email = CreateSyntheticEmail(cprUuid);
        var claims = principal.Claims.ToList();
        AddOrReplace(claims, ClaimTypes.NameIdentifier, cprUuid);
        AddOrReplace(claims, ClaimTypes.Email, email);
        AddOrReplace(claims, ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? cprUuid : fullName);
        AddOrReplace(claims, NemLogin3MemberLoginConstants.ProfileDataClaimType, JsonSerializer.Serialize(CreateProfile(principal, cprUuid, fullName, email), JsonSerializerOptions.Web));

        var authenticationType = principal.Identity?.AuthenticationType ?? NemLogin3MemberLoginConstants.SchemeName;
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

    private static NemLogin3MemberProfile CreateProfile(ClaimsPrincipal principal, string cprUuid, string? fullName, string email)
        => new()
        {
            CprUuid = cprUuid,
            FullName = fullName,
            Email = email,
            NsisLoa = principal.FindFirstValue(NemLogin3ClaimConstants.NsisLoa),
            ProfessionalCvr = principal.FindFirstValue(NemLogin3ClaimConstants.ProfessionalCvr),
            ProfessionalOrgName = principal.FindFirstValue(NemLogin3ClaimConstants.ProfessionalOrgName),
        };

    private static void AddOrReplace(List<Claim> claims, string type, string value)
    {
        claims.RemoveAll(claim => claim.Type == type);
        claims.Add(new Claim(type, value));
    }
}
