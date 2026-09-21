using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    public class TorneosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
