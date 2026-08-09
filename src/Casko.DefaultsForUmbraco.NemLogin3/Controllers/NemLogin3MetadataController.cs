using Casko.Authentication.NemLogin3.Web.Services;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casko.DefaultsForUmbraco.NemLogin3.Controllers;

[AllowAnonymous]
[Route("Metadata")]
public sealed class NemLogin3MetadataController(INemLogin3MetadataService metadataService) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var entityDescriptor = metadataService.CreateMetadata(Request);
        return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
    }
}
