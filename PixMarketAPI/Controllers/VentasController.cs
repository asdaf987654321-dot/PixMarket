using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using PixMarketAPI.Models;

namespace PixMarketAPI.Controllers
{
    [ApiController]
    [Route("api/ventas")]
    public class VentasController : ControllerBase
    {
        private readonly PixContext _context;

        public VentasController(PixContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Finaliza una venta: valida el stock disponible, lo descuenta
        /// y devuelve el total. El carrito llega como lista de líneas
        /// (IdItem + Cantidad).
        /// </summary>
        /// <param name="request">Cuerpo JSON con las líneas del carrito.</param>
        /// <returns>Resultado de la operación con el total en bolivianos.</returns>
        [HttpPost("finalizar")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(FinalizarVentaResultado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FinalizarVentaResultado), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FinalizarVentaResultado), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(FinalizarVentaResultado), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FinalizarVentaResultado>> Finalizar(
            [FromBody] FinalizarVentaRequest request)
        {
            var lineas = request.Lineas
                .Where(l => l.Cantidad > 0)
                .ToList();

            if (!lineas.Any())
            {
                return BadRequest(new FinalizarVentaResultado
                {
                    Ok = false,
                    Mensaje = "El carrito está vacío.",
                    Total = 0
                });
            }

            var ids = lineas.Select(l => l.IdItem).ToList();

            var items = await _context.Items
                .Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            var problemas = new List<string>();

            foreach (var linea in lineas)
            {
                if (!items.TryGetValue(linea.IdItem, out var item))
                {
                    problemas.Add($"Producto #{linea.IdItem} (ya no está disponible)");
                }
                else if (item.Stock < linea.Cantidad)
                {
                    problemas.Add($"{item.Nombre} (solo hay {item.Stock} en stock)");
                }
            }

            if (problemas.Any())
            {
                return Conflict(new FinalizarVentaResultado
                {
                    Ok = false,
                    Mensaje = "No se pudo finalizar la venta. Sin stock suficiente para: " +
                        string.Join(", ", problemas) + ".",
                    Total = 0
                });
            }

            var total = 0m;

            foreach (var linea in lineas)
            {
                var item = items[linea.IdItem];
                item.Stock -= linea.Cantidad;
                total += item.Precio * linea.Cantidad;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new FinalizarVentaResultado
                    {
                        Ok = false,
                        Mensaje = "No se pudo finalizar la venta. " +
                            "Verifica que la base de datos esté disponible.",
                        Total = 0
                    });
            }

            return Ok(new FinalizarVentaResultado
            {
                Ok = true,
                Mensaje = "Venta finalizada correctamente. " +
                    $"Total: Bs {total.ToString("N2")}. El stock fue actualizado.",
                Total = total
            });
        }
    }
}