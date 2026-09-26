using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class InventarioController : Controller
    {
        private readonly IPixMarketApiService _api;

        public InventarioController(IPixMarketApiService api)
        {
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}