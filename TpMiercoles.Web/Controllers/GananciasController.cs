using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public class GananciasController : Controller
    {
        [HttpGet("/ganancias")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
