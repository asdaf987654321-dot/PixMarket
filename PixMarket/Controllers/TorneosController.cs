using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    public class TorneosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Inscribirse()
        {
            return View();
        }

        public IActionResult Calendario()
        {
            return View();
        }
    }
}
