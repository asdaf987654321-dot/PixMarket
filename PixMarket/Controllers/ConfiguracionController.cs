using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ConfiguracionController : Controller
    {
        private const string MessageApiCaida =
            "No se pudo obtener la configuración. " +
            "Verifica que PixMarketAPI esté en ejecución y que la base de datos " +
            "tenga creada la tabla 'configuraciones'.";

        private readonly IPixMarketApiService _api;

        public ConfiguracionController(IPixMarketApiService api)
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
                ViewBag.Error = MessageApiCaida;
                return View(new Configuracion());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(Configuracion configuracion)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", configuracion);
            }

            try
            {
                var resultado = await _api.GuardarConfiguracionAsync(configuracion);

                if (!resultado.Ok)
                {
                    ViewBag.Error = resultado.Mensaje ??
                        "No se pudo guardar la configuración.";

                    return View("Index", configuracion);
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = MessageApiCaida;
                return View("Index", configuracion);
            }

            TempData["Mensaje"] = "Configuración guardada correctamente.";
            return RedirectToAction(nameof(Index));
        }


    }
}