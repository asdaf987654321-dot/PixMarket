using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    public class UsuarioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
