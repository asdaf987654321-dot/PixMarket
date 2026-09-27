using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class PedidosController : Controller
    {
        private readonly IPixMarketApiService _apiService;

        public PedidosController(IPixMarketApiService apiService)
        {
            _apiService = apiService;
        }

        
        public async Task<IActionResult> Index()
        {
            var pedidos = await _apiService.ObtenerPedidosAdminAsync();
            return View(pedidos ?? new List<VentaDto>());
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