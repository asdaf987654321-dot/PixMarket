using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly IPixMarketApiService _api;

        public CategoriasController(IPixMarketApiService api)
        {
            _api = api;
        }

        // Vista de TIENDA
        public async Task<IActionResult> Index(
            string? buscar,
            string? juego,
            string[]? juegos,
            string[]? categorias,
            string[]? rarezas,
            decimal? precioMin,
            decimal? precioMax)
        {
            var resultado = new TiendaResultadoDto();

            try
            {
                resultado = await _api.ObtenerTiendaAsync(
                    buscar, juego, juegos, categorias, rarezas, precioMin, precioMax);
            }
            catch (HttpRequestException)
            {
                ViewBag.ApiError =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";
            }

            // Estado de los filtros (para que queden marcados)
            ViewBag.Buscar = string.IsNullOrWhiteSpace(buscar) ? "" : buscar.Trim();
            ViewBag.Juego = string.IsNullOrWhiteSpace(juego) ? "" : juego.Trim();
            ViewBag.SelJuegos =
                string.IsNullOrWhiteSpace(juego)
                    ? (juegos ?? Array.Empty<string>()).ToList()
                    : new List<string> { juego.Trim() };
            ViewBag.SelCategorias = (categorias ?? Array.Empty<string>()).ToList();
            ViewBag.SelRarezas = (rarezas ?? Array.Empty<string>()).ToList();
            ViewBag.PrecioMin = precioMin;
            ViewBag.PrecioMax = precioMax;
            ViewBag.PrecioMaximoReal = resultado.PrecioMaximoReal;

            // Contadores de pestañas (sobre el total de la tienda)
            ViewBag.TotalItems = resultado.TotalItems;
            ViewBag.CYgo = resultado.CYgo;
            ViewBag.CPokemon = resultado.CPokemon;
            ViewBag.CMagic = resultado.CMagic;
            ViewBag.COtros = resultado.COtros;

            return View(resultado.Items);
        }

        // Vista de CATEGORÍAS
        public IActionResult Categorias()
        {
            return View();
        }
    }
}