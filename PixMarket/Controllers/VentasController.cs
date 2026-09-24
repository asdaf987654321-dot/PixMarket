using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class VentasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}