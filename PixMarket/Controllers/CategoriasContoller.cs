using Microsoft.AspNetCore.Mvc;

namespace PixMarket.Controllers
{
    public class CategoriasController : Controller
    {
        // Vista de TIENDA
        public IActionResult Index()
        {
            return View();
        }

        // Vista de CATEGORÍAS
        public IActionResult Categorias()
        {
            return View();
        }
    }
}