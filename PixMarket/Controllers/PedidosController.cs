using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class PedidosController : Controller
    {
        private const string MessageApiCaida =
            "No se pudo obtener la lista de pedidos. " +
            "Verifica que PixMarketAPI esté en ejecución y que la base de datos " +
            "tenga el esquema actualizado.";

        private readonly IPixMarketApiService _apiService;

        public PedidosController(IPixMarketApiService apiService)
        {
            _apiService = apiService;
        }

        
        public async Task<IActionResult> Index()
        {
            try
            {
                var pedidos = await _apiService.ObtenerPedidosAdminAsync();
                return View(pedidos ?? new List<VentaDto>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = MessageApiCaida;
                return View(new List<VentaDto>());
            }
        }

       
        public async Task<IActionResult> Detalle(int id)
        {
            VentaDetalleDto? pedido;

            try
            {
                pedido = await _apiService.ObtenerDetallePedidoAsync(id);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = MessageApiCaida;
                return RedirectToAction(nameof(Index));
            }

            if (pedido == null)
            {
                return NotFound();
            }
            return View(pedido); 
        }

        
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            bool resultado;

            try
            {
                resultado = await _apiService.ActualizarEstadoPedidoAsync(id, estado);
            }
            catch (HttpRequestException)
            {
                resultado = false;
            }

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