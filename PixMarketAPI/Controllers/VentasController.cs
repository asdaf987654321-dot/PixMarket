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

            // Por defecto el pedido queda "Pendiente". El panel de administración
            // puede pedir que nazca ya "Entregado" (compra en mostrador), así el
            // administrador no tiene que ir despues a Pedidos a marcarlo.
            var estadoPedido = string.IsNullOrWhiteSpace(request.Estado)
                ? "Pendiente"
                : request.Estado.Trim();

            if (estadoPedido != "Pendiente" && estadoPedido != "Entregado")
            {
                return BadRequest(new FinalizarVentaResultado
                {
                    Ok = false,
                    Mensaje = "El estado inicial debe ser 'Pendiente' o 'Entregado'.",
                    Total = 0
                });
            }

            var nuevaVenta = new Venta
            {
                IdUsuario = request.IdUsuario,
                FechaVenta = DateTime.Now,
                Total = 0m,
                MetodoPago = "General",
                Estado = estadoPedido,
                FechaActualizacion = estadoPedido == "Entregado" ? DateTime.Now : null
            };

            _context.Ventas.Add(nuevaVenta);

            var listaDetalles = new List<DetalleVenta>();

            foreach (var linea in lineas)
            {
                var item = items[linea.IdItem];

                item.Stock -= linea.Cantidad;

                var subtotal = item.Precio * linea.Cantidad;
                total += subtotal;

                listaDetalles.Add(new DetalleVenta
                {
                    Venta = nuevaVenta,
                    IdItem = item.Id,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = item.Precio,
                    Subtotal = subtotal
                });
            }

            nuevaVenta.Total = total;
            _context.DetallesVenta.AddRange(listaDetalles);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new FinalizarVentaResultado
                    {
                        Ok = false,
                        Mensaje = $"Error de BD: {errorReal}",
                        Total = 0
                    });
            }

            return Ok(new FinalizarVentaResultado
            {
                Ok = true,
                Mensaje = "Venta finalizada correctamente. " +
                    $"Total: Bs {total.ToString("N2")}. El stock fue actualizado." +
                    (estadoPedido == "Entregado" ? " El pedido quedo como Entregado." : ""),
                Total = total
            });
        }

      
        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<VentaDto>>> GetPedidosAdmin()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Usuario)
                .OrderByDescending(v => v.FechaVenta)
                .Select(v => new VentaDto
                {
                    Id = v.Id,
                    NombreUsuario = v.Usuario != null ? v.Usuario.Nombre : "Desconocido",
                    FechaVenta = v.FechaVenta,
                    Total = v.Total,
                    Estado = v.Estado
                })
                .ToListAsync();

            return Ok(ventas);
        }

       
        [HttpGet("{id}")]
        public async Task<ActionResult<VentaDetalleDto>> GetDetallePedido(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Item)
                .Where(v => v.Id == id)
                .Select(v => new VentaDetalleDto
                {
                    Id = v.Id,
                    NombreUsuario = v.Usuario != null ? v.Usuario.Nombre : "Desconocido",
                    FechaVenta = v.FechaVenta,
                    Total = v.Total,
                    Estado = v.Estado,
                    Lineas = v.DetallesVenta.Select(d => new LineaVentaDetalleDto
                    {
                        NombreItem = d.Item != null ? d.Item.Nombre : "Producto",
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Cantidad * d.PrecioUnitario
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (venta == null) return NotFound();

            return Ok(venta);
        }


        [HttpGet("ventas-recientes")]
        public async Task<IActionResult> GetVentasRecientes()
        {
            try
            {
                var ventas = await _context.Ventas
                    .Where(v => v.Estado == "Entregado") 
                    .OrderByDescending(v => v.FechaVenta)
                    .Take(30) 
                    .Select(v => new VentaDto
                    {
                        Id = v.Id,
                        NombreUsuario = v.Usuario != null ? v.Usuario.Nombre : "Cliente",
                        FechaVenta = v.FechaVenta,
                        Total = v.Total,
                        Estado = v.Estado
                    })
                    .ToListAsync();

                return Ok(ventas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<VentasStatsDto>> GetVentasStats()
        {
            var hoy = DateTime.Today;

            
            int diferencia = (int)hoy.DayOfWeek - (int)DayOfWeek.Monday;
            if (diferencia < 0) diferencia += 7;
            var inicioSemana = hoy.AddDays(-diferencia).Date;

            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            
            var ventasDelDia = await _context.Ventas
                .Where(v => v.Estado == "Entregado" &&
                       ((v.FechaActualizacion != null && v.FechaActualizacion.Value.Date == hoy) ||
                        (v.FechaActualizacion == null && v.FechaVenta.Date == hoy)))
                .SumAsync(v => (decimal?)v.Total) ?? 0m;

            
            var ventasDeLaSemana = await _context.Ventas
                .Where(v => v.Estado == "Entregado" &&
                       ((v.FechaActualizacion != null && v.FechaActualizacion.Value.Date >= inicioSemana) ||
                        (v.FechaActualizacion == null && v.FechaVenta.Date >= inicioSemana)))
                .SumAsync(v => (decimal?)v.Total) ?? 0m;

            
            var pedidosEnElMes = await _context.Ventas
                .Where(v => v.Estado == "Entregado" &&
                       ((v.FechaActualizacion != null && v.FechaActualizacion.Value.Date >= inicioMes) ||
                        (v.FechaActualizacion == null && v.FechaVenta.Date >= inicioMes)))
                .CountAsync();

            
            var totalMes = await _context.Ventas
                .Where(v => v.Estado == "Entregado" &&
                       ((v.FechaActualizacion != null && v.FechaActualizacion.Value.Date >= inicioMes) ||
                        (v.FechaActualizacion == null && v.FechaVenta.Date >= inicioMes)))
                .SumAsync(v => (decimal?)v.Total) ?? 0m;

            var ticketPromedio = pedidosEnElMes > 0 ? totalMes / pedidosEnElMes : 0m;

            return Ok(new VentasStatsDto
            {
                VentasDelDia = ventasDelDia,
                VentasDeLaSemana = ventasDeLaSemana,
                PedidosEnElMes = pedidosEnElMes,
                TicketPromedio = ticketPromedio
            });
        }

        
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
        {
            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null) return NotFound();

            venta.Estado = request.Estado;

            
            if (request.Estado == "Entregado")
            {
                venta.FechaActualizacion = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}