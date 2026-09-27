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


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _api.ObtenerEstadisticasInventarioAsync() ?? new ApiInventarioStats();
                var items = await _api.ObtenerItemsInventarioAsync();

                if (items != null)
                {
                    stats.Items = items;
                }

                return View(stats);
            }
            catch
            {
                return View(new ApiInventarioStats());
            }
        }
    }
}