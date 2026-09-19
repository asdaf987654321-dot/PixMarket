using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    public class CategoriasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}