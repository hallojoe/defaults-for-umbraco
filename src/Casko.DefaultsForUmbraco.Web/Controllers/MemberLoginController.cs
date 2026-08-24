using Microsoft.AspNetCore.Mvc;

namespace Casko.DefaultsForUmbraco.Web.Controllers;

/// <summary>
/// Starts the configured external member sign-in flow without requiring a content login page.
/// </summary>
[Route("member-login")]
public sealed class MemberLoginController : Controller
{
    /// <summary>
    /// Renders the short-lived form that starts Umbraco's external member login flow.
    /// </summary>
    [HttpGet]
    public IActionResult Index(string? returnUrl)
    {
        var localReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";

        return View("~/Views/MemberLogin.cshtml", localReturnUrl);
    }
}
