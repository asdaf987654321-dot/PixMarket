using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarket.Data;
using PixMarket.Models;
using System.Text.Json;

namespace PixMarket.Controllers
{
    public class CarritoController : Controller
    {
        private const string SessionKey = "Carrito";

        private readonly PixContext _context;

        public CarritoController(PixContext context)
        {
            _context = context;
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
            var item = await _context.Items.FindAsync(id);
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

        // Finaliza la venta: descuenta el stock de los items
        // y vacía el carrito. Requiere sesión iniciada (cualquier rol);
        // el stock se descuenta directamente.
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

            var ids = carrito.Select(c => c.Id).ToList();
            var items = await _context.Items
                .Where(i => ids.Contains(i.Id))
                .ToListAsync();

            var problemas = new List<string>();

            foreach (var linea in carrito)
            {
                var item = items.FirstOrDefault(i => i.Id == linea.Id);

                if (item == null)
                {
                    problemas.Add($"{linea.Nombre} (ya no está disponible)");
                }
                else if (item.Stock < linea.Cantidad)
                {
                    problemas.Add($"{item.Nombre} (solo hay {item.Stock} en stock)");
                }
            }

            if (problemas.Any())
            {
                TempData["MensajeCarrito"] =
                    "No se pudo finalizar la venta. Sin stock suficiente para: " +
                    string.Join(", ", problemas) + ".";

                return RedirectToAction(nameof(Index));
            }

            var total = carrito.Sum(l => l.Subtotal);

            foreach (var linea in carrito)
            {
                var item = items.First(i => i.Id == linea.Id);
                item.Stock -= linea.Cantidad;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(SessionKey);

            TempData["MensajeCarrito"] =
                $"Venta finalizada correctamente. Total: Bs {total.ToString("N2")}. " +
                "El stock fue actualizado.";

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