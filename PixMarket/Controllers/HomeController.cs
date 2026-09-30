using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;
using PixMarket.Servicios;
using System.Diagnostics;

namespace PixMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPixMarketApiService _api;

        public HomeController(ILogger<HomeController> logger, IPixMarketApiService api)
        {
            _logger = logger;
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var destacados = new List<ProductoDestacadoDto>();

            try
            {
                destacados = await _api.ObtenerDestacadosAsync();
            }
            catch (HttpRequestException)
            {
                ViewBag.ApiError =
                    "No se pudieron cargar los productos destacados. " +
                    "Verifica que PixMarketAPI esté en ejecución.";
            }

            return View(destacados);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}