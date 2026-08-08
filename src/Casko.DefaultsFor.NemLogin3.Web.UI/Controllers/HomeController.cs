using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casko.DefaultsFor.NemLogin3.Web.UI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Secure()
        {
            // The NameIdentifier
            var nameIdentifier = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).Single();

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
