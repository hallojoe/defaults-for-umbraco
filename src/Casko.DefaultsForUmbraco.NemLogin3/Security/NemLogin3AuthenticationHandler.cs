using System.Security.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Casko.Authentication.NemLogin3.Web.Configuration;
using Casko.Authentication.NemLogin3.Web.Services;
using Casko.DefaultsForUmbraco.NemLogin3.Configuration;
using Casko.DefaultsForUmbraco.NemLogin3.Services;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casko.DefaultsForUmbraco.NemLogin3.Security;

public abstract class NemLogin3AuthenticationHandlerBase(
    IOptionsMonitor<NemLogin3AuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDataProtectionProvider dataProtectionProvider,
    Saml2Configuration saml2Configuration,
    IOptions<NemLogin3Options> nemLoginOptions,
    INemLogin3ClaimsTransformer claimsTransformer,
    IDistributedCache distributedCache)
    : RemoteAuthenticationHandler<NemLogin3AuthenticationOptions>(options, logger, encoder)
{
    private const string RelayStateCachePrefix = "Casko.DefaultsForUmbraco.NemLogin3.RelayState:";
    private const string RelayStateSchemeKey = "Scheme";
    private static readonly TimeSpan RelayStateCacheDuration = TimeSpan.FromMinutes(15);

    private readonly Saml2Configuration _saml2Configuration = saml2Configuration;
    private readonly NemLogin3Options _nemLoginOptions = nemLoginOptions.Value;
    private readonly INemLogin3ClaimsTransformer _claimsTransformer = claimsTransformer;
    private readonly IDataProtectionProvider _dataProtectionProvider = dataProtectionProvider;
    private readonly IDistributedCache _distributedCache = distributedCache;

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        PrepareChallengeProperties(properties);
        GenerateCorrelationId(properties);
        var stateKey = Guid.NewGuid().ToString("N");
        await _distributedCache.SetStringAsync(
            CreateRelayStateCacheKey(stateKey),
            CreateStateDataFormat().Protect(properties),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = RelayStateCacheDuration,
            },
            Context.RequestAborted);

        var binding = new Saml2RedirectBinding();
        binding.SetRelayStateQuery(new Dictionary<string, string>
        {
            [NemLogin3MemberLoginConstants.RelayStateKey] = stateKey,
            [RelayStateSchemeKey] = Scheme.Name,
        });

        var request = new Saml2AuthnRequest(_saml2Configuration)
        {
            AssertionConsumerServiceUrl = new Uri(BuildRedirectUri(Options.CallbackPath)),
            NameIdPolicy = new NameIdPolicy
            {
                AllowCreate = true,
                Format = NameIdentifierFormats.Persistent.OriginalString
            },
            RequestedAuthnContext = new RequestedAuthnContext
            {
                Comparison = GetAuthnContextComparisonType(),
                AuthnContextClassRef = [_nemLoginOptions.RequestedAuthnContext],
            }
        };

        binding.Bind(request);
        Response.Redirect(binding.RedirectLocation.OriginalString);
    }

    public override async Task<bool> ShouldHandleRequestAsync()
    {
        if (!await base.ShouldHandleRequestAsync())
        {
            return false;
        }

        var relayStateScheme = await ReadRelayStateSchemeAsync();
        return string.IsNullOrWhiteSpace(relayStateScheme)
               || string.Equals(relayStateScheme, Scheme.Name, StringComparison.Ordinal);
    }

    protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        try
        {
            var httpRequest = Request.ToGenericHttpRequest(validate: true);
            var response = new Saml2AuthnResponse(_saml2Configuration);

            httpRequest.Binding.ReadSamlResponse(httpRequest, response);
            if (response.Status != Saml2StatusCodes.Success)
            {
                return HandleRequestResult.Fail($"SAML response status: {response.Status}");
            }

            httpRequest.Binding.Unbind(httpRequest, response);

            var relayState = httpRequest.Binding.GetRelayStateQuery();
            if (!relayState.TryGetValue(NemLogin3MemberLoginConstants.RelayStateKey, out var stateKey))
            {
                return HandleRequestResult.Fail("Missing NemLog-in relay state.");
            }

            var relayStateCacheKey = CreateRelayStateCacheKey(stateKey);
            var protectedState = await _distributedCache.GetStringAsync(relayStateCacheKey, Context.RequestAborted);
            if (string.IsNullOrWhiteSpace(protectedState))
            {
                return HandleRequestResult.Fail("Expired or unknown NemLog-in relay state.");
            }

            await _distributedCache.RemoveAsync(relayStateCacheKey, Context.RequestAborted);

            var properties = CreateStateDataFormat().Unprotect(protectedState);
            if (properties is null)
            {
                return HandleRequestResult.Fail("Invalid NemLog-in relay state.");
            }

            if (!ValidateCorrelationId(properties))
            {
                return HandleRequestResult.Fail("Invalid NemLog-in correlation state.");
            }

            var principal = new ClaimsPrincipal(response.ClaimsIdentity);
            principal = _claimsTransformer.Transform(principal);
            principal = MapClaims(principal);

            return HandleRequestResult.Success(new AuthenticationTicket(principal, properties, Scheme.Name));
        }
        catch (AuthenticationException exception)
        {
            return HandleRequestResult.Fail(exception);
        }
        catch (Exception exception)
        {
            return HandleRequestResult.Fail(exception);
        }
    }

    private AuthnContextComparisonTypes GetAuthnContextComparisonType()
        => Enum.TryParse<AuthnContextComparisonTypes>(_nemLoginOptions.RequestedAuthnContextComparison, ignoreCase: true, out var comparison)
            ? comparison
            : AuthnContextComparisonTypes.Minimum;

    protected abstract ClaimsPrincipal MapClaims(ClaimsPrincipal principal);

    protected virtual void PrepareChallengeProperties(AuthenticationProperties properties)
    {
    }

    private ISecureDataFormat<AuthenticationProperties> CreateStateDataFormat()
        => new PropertiesDataFormat(_dataProtectionProvider.CreateProtector(
            typeof(NemLogin3AuthenticationHandlerBase).FullName!,
            Scheme.Name,
            "RelayState"));

    private string CreateRelayStateCacheKey(string stateKey)
        => $"{RelayStateCachePrefix}{Scheme.Name}:{stateKey}";

    private async Task<string?> ReadRelayStateSchemeAsync()
    {
        var relayState = Request.Query.TryGetValue("RelayState", out var queryRelayState)
            ? queryRelayState.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(relayState) && Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            if (form.TryGetValue("RelayState", out var formRelayState))
            {
                relayState = formRelayState.ToString();
            }
        }

        if (string.IsNullOrWhiteSpace(relayState))
        {
            return null;
        }

        var relayStateValues = QueryHelpers.ParseQuery(relayState);
        return relayStateValues.TryGetValue(RelayStateSchemeKey, out var scheme)
            ? scheme.ToString()
            : null;
    }
}

public sealed class NemLogin3AuthenticationHandler(
    IOptionsMonitor<NemLogin3AuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDataProtectionProvider dataProtectionProvider,
    Saml2Configuration saml2Configuration,
    IOptions<NemLogin3Options> nemLoginOptions,
    INemLogin3ClaimsTransformer claimsTransformer,
    INemLogin3MemberClaimsMapper memberClaimsMapper,
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
    private readonly INemLogin3MemberClaimsMapper _memberClaimsMapper = memberClaimsMapper;

    protected override ClaimsPrincipal MapClaims(ClaimsPrincipal principal)
        => _memberClaimsMapper.Map(principal);
}
