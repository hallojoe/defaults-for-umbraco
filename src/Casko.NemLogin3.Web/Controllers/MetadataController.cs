using Casko.NemLogin3.Web.Services;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casko.NemLogin3.Web.Controllers;

[AllowAnonymous]
[Route("Metadata")]
public class MetadataController : Controller
{
    private readonly INemLogin3MetadataService metadataService;

    public MetadataController(INemLogin3MetadataService metadataService)
    {
        this.metadataService = metadataService;
    }

    public IActionResult Index()
    {
        var entityDescriptor = metadataService.CreateMetadata(Request);
        return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
    }
}
