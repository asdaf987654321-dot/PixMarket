using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    public class ContactoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}