using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;
using System.Security.Claims;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class VentasController : Controller
    {
        private readonly IPixMarketApiService _apiService;

        public VentasController(IPixMarketApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index() 
        {
            var ventas = await _apiService.GetVentasRecientesAsync();

            var stats = await _apiService.ObtenerEstadisticasVentasAsync();

            ViewBag.Stats = stats ?? new VentasStatsDto();


            return View(ventas);
        }


        public async Task<IActionResult> Detalle(int id)
        {
            var pedido = await _apiService.ObtenerDetallePedidoAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }
            return View(pedido); 
        }


        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var resultado = await _apiService.ActualizarEstadoPedidoAsync(id, estado);
            if (resultado)
            {
                TempData["Mensaje"] = "El estado del pedido se actualizó correctamente.";
            }
            else
            {
                TempData["Error"] = "No fue posible actualizar el estado del pedido.";
            }
            return RedirectToAction(nameof(Index));
        }



    }
}