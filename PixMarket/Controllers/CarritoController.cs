using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;
using PixMarket.Servicios;
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

        // Lista el carrito
        public IActionResult Index()
        {
            return View(ObtenerCarrito());
        }

        // Agrega un item (o suma cantidad)
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

        // Actualiza la cantidad de un item
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

        // Elimina un item del carrito
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

        // Vacía el carrito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            HttpContext.Session.Remove(SessionKey);
            return RedirectToAction(nameof(Index));
        }

        // Finaliza la venta: la API valida el stock, lo descuenta
        // y la app vacía el carrito. Requiere sesión iniciada.
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

            FinalizarVentaDto resultado;

            try
            {
                resultado = await _api.FinalizarVentaAsync(carrito);
            }
            catch (HttpRequestException)
            {
                TempData["MensajeCarrito"] =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";

                return RedirectToAction(nameof(Index));
            }

            if (resultado.Ok)
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
                    string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "No se pudo finalizar la venta."
                        : resultado.Mensaje;
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