using System.Security.Authentication;
using Casko.NemLogin3.Web.Configuration;
using Casko.NemLogin3.Web.Services;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Casko.NemLogin3.Web.Controllers;

[AllowAnonymous]
[Route("Auth")]
public class AuthController : Controller
{
    private const string RelayStateReturnUrl = "ReturnUrl";

    private readonly Saml2Configuration _config;
    private readonly NemLogin3Options _options;
    private readonly INemLogin3ClaimsTransformer _claimsTransformer;

    public AuthController(
        Saml2Configuration config,
        IOptions<NemLogin3Options> options,
        INemLogin3ClaimsTransformer claimsTransformer)
    {
        _config = config;
        _options = options.Value;
        _claimsTransformer = claimsTransformer;
    }

    [Route("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var binding = new Saml2RedirectBinding();
        binding.SetRelayStateQuery(new Dictionary<string, string> { { RelayStateReturnUrl, returnUrl ?? Url.Content("~/") } });

        var resultBinding = binding.Bind(new Saml2AuthnRequest(_config)
        {
            NameIdPolicy = new NameIdPolicy
            {
                AllowCreate = true,
                Format = NameIdentifierFormats.Persistent.OriginalString
            },
            RequestedAuthnContext = new RequestedAuthnContext
            {
                Comparison = GetAuthnContextComparisonType(),
                AuthnContextClassRef = [_options.RequestedAuthnContext],
            }
        });

        return resultBinding.ToActionResult();
    }

    [Route("AssertionConsumerService")]
    public async Task<IActionResult> AssertionConsumerService()
    {
        var httpRequest = Request.ToGenericHttpRequest(validate: true);
        var saml2AuthnResponse = new Saml2AuthnResponse(_config);

        httpRequest.Binding.ReadSamlResponse(httpRequest, saml2AuthnResponse);
        if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
        {
            throw new AuthenticationException($"SAML Response status: {saml2AuthnResponse.Status}");
        }

        httpRequest.Binding.Unbind(httpRequest, saml2AuthnResponse);
        await saml2AuthnResponse.CreateSessionAsync(
            HttpContext,
            claimsTransform: claimsPrincipal => Task.FromResult(_claimsTransformer.Transform(claimsPrincipal)));

        var relayStateQuery = httpRequest.Binding.GetRelayStateQuery();
        var returnUrl = relayStateQuery.TryGetValue(RelayStateReturnUrl, out var relayStateReturnUrl)
            ? relayStateReturnUrl
            : Url.Content("~/");

        return Redirect(returnUrl);
    }

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated is not true)
        {
            return Redirect(Url.Content("~/"));
        }

        var binding = new Saml2PostBinding();
        var saml2LogoutRequest = await new Saml2LogoutRequest(_config, User).DeleteSession(HttpContext);
        return binding.Bind(saml2LogoutRequest).ToActionResult();
    }

    [Route("LoggedOut")]
    public IActionResult LoggedOut()
    {
        var httpRequest = Request.ToGenericHttpRequest(validate: true);
        httpRequest.Binding.Unbind(httpRequest, new Saml2LogoutResponse(_config));

        return Redirect(Url.Content("~/"));
    }

    [Route("SingleLogout")]
    public async Task<IActionResult> SingleLogout()
    {
        Saml2StatusCodes status;
        var httpRequest = Request.ToGenericHttpRequest(validate: true);
        var logoutRequest = new Saml2LogoutRequest(_config, User);

        try
        {
            httpRequest.Binding.Unbind(httpRequest, logoutRequest);
            status = Saml2StatusCodes.Success;
            await logoutRequest.DeleteSession(HttpContext);
        }
        catch
        {
            status = Saml2StatusCodes.RequestDenied;
        }

        var responseBinding = new Saml2PostBinding
        {
            RelayState = httpRequest.Binding.RelayState
        };
        var saml2LogoutResponse = new Saml2LogoutResponse(_config)
        {
            InResponseToAsString = logoutRequest.IdAsString,
            Status = status,
        };

        return responseBinding.Bind(saml2LogoutResponse).ToActionResult();
    }

    private AuthnContextComparisonTypes GetAuthnContextComparisonType()
    {
        return Enum.TryParse<AuthnContextComparisonTypes>(_options.RequestedAuthnContextComparison, ignoreCase: true, out var comparison)
            ? comparison
            : AuthnContextComparisonTypes.Minimum;
    }
}
