using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    public class TiendaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
