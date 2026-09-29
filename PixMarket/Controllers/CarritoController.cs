using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;
using PixMarket.Servicios;
using System.Security.Claims;
using System.Text.Json;

namespace PixMarket.Controllers
{
    public class CarritoController : Controller
    {
        private const string SessionKey = "Carrito";

        private readonly IPixMarketApiService _api;

        public CarritoController(IPixMarketApiService api)
        {
            _api = api;
        }

       
        public IActionResult Index()
        {
            return View(ObtenerCarrito());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int id, int cantidad = 1)
        {

            Item? item;

            try
            {
                item = await _api.ObtenerItemAsync(id);
            }
            catch (HttpRequestException)
            {
                TempData["MensajeCarrito"] =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";

                return RedirectToAction("Index", "Categorias");
            }

            if (item == null)
            {
                return NotFound();
            }

            cantidad = Math.Max(1, cantidad);

            if (item.Stock <= 0)
            {
                TempData["MensajeCarrito"] = "Este producto está agotado.";
                return RedirectToAction("Index", "Categorias");
            }

            var carrito = ObtenerCarrito();
            var existente = carrito.FirstOrDefault(c => c.Id == id);

            if (existente != null)
            {
                existente.Cantidad = Math.Min(existente.Cantidad + cantidad, item.Stock);
            }
            else
            {
                carrito.Add(new ItemCarrito
                {
                    Id = item.Id,
                    Nombre = item.Nombre,
                    Juego = item.Juego,
                    Categoria = item.Categoria,
                    Rareza = item.Rareza,
                    Precio = item.Precio,
                    ImagenRuta = item.ImagenRuta,
                    Stock = item.Stock,
                    Cantidad = Math.Min(cantidad, item.Stock)
                });
            }

            GuardarCarrito(carrito);

            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCantidad(int id, int cantidad)
        {
            var carrito = ObtenerCarrito();
            var existente = carrito.FirstOrDefault(c => c.Id == id);

            if (existente != null)
            {
                if (cantidad <= 0)
                {
                    carrito.Remove(existente);
                }
                else
                {
                    existente.Cantidad = Math.Min(cantidad, existente.Stock);
                }

                GuardarCarrito(carrito);
            }

            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            var carrito = ObtenerCarrito();
            var existente = carrito.FirstOrDefault(c => c.Id == id);

            if (existente != null)
            {
                carrito.Remove(existente);
                GuardarCarrito(carrito);
            }

            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            HttpContext.Session.Remove(SessionKey);
            return RedirectToAction(nameof(Index));
        }

        
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar()
        {
            

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["MensajeCarrito"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Index));
            }

            
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int idUsuario))
            {
                TempData["MensajeCarrito"] = "Debes iniciar sesión correctamente para finalizar la compra.";
                return RedirectToAction("Login", "Usuarios");
            }

            
            var request = new FinalizarVentaRequest
            {
                IdUsuario = idUsuario, 
                Lineas = carrito.Select(c => new LineaVentaRequest
                {
                    IdItem = c.Id,
                    Cantidad = c.Cantidad
                }).ToList(),

                // Si compra un administrador, el pedido nace ya entregado:
                // no hace falta ir despues a Pedidos a marcarlo.
                Estado = User.IsInRole("Administrador") ? "Entregado" : null
            };

            FinalizarVentaResultado? resultado;

            try
            {
                resultado = await _api.FinalizarVentaAsync(request);
            }
            catch (HttpRequestException)
            {
                TempData["MensajeCarrito"] =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";

                return RedirectToAction(nameof(Index));
            }

            if (resultado != null && resultado.Ok)
            {
                HttpContext.Session.Remove(SessionKey);

                TempData["MensajeCarrito"] =
                    string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "Venta finalizada correctamente."
                        : resultado.Mensaje;
            }
            else
            {
                TempData["MensajeCarrito"] =
                    string.IsNullOrWhiteSpace(resultado?.Mensaje)
                        ? "No se pudo finalizar la venta."
                        : resultado?.Mensaje;
            }

            return RedirectToAction(nameof(Index));
        }

        
        private List<ItemCarrito> ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString(SessionKey);

            if (string.IsNullOrEmpty(json))
            {
                return new List<ItemCarrito>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<ItemCarrito>>(json) ?? new List<ItemCarrito>();
            }
            catch (JsonException)
            {
                return new List<ItemCarrito>();
            }
        }

        private void GuardarCarrito(List<ItemCarrito> carrito)
        {
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(carrito));
        }
    }
}