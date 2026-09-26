using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    public class ContactoController : Controller
    {
        private readonly IPixMarketApiService _api;

        public ContactoController(IPixMarketApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var configuracion = await _api.ObtenerConfiguracionAsync();
                return View(configuracion);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo obtener la información de contacto.";

                return View(new Configuracion());
            }
        }
    }
}