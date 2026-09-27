using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using PixMarketAPI.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace PixMarketAPI.Controllers
{
    [Route("api/reportes")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        private readonly PixContext _context;

        public ReportesController(PixContext context)
        {
            _context = context;
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new { productos = new List<object>(), usuarios = new List<object>() });
            }

            q = q.ToLower();

            // Validación de nulos para evitar CS8602 en p.Nombre y p.Juego
            var productos = await _context.Items
                .Where(p => (p.Nombre != null && p.Nombre.ToLower().Contains(q)) ||
                            (p.Juego != null && p.Juego.ToLower().Contains(q)))
                .Take(5)
                .Select(p => new {
                    p.Id,
                    Nombre = p.Nombre ?? string.Empty,
                    Tipo = "Producto",
                    Url = $"/Items/Details/{p.Id}"
                })
                .ToListAsync();

            // Validación de nulos para u.Nombre y u.Correo
            var usuarios = await _context.Usuarios
                .Where(u => (u.Nombre != null && u.Nombre.ToLower().Contains(q)) ||
                            (u.Correo != null && u.Correo.ToLower().Contains(q)))
                .Take(5)
                .Select(u => new {
                    u.Id,
                    Nombre = u.Nombre ?? string.Empty,
                    Tipo = "Usuario",
                    Url = "/Usuarios/Administrador"
                })
                .ToListAsync();

            var resultados = productos.Cast<object>().Concat(usuarios).ToList();

            return Ok(resultados);
        }

        [HttpGet("general")]
        public async Task<IActionResult> GetReporteGeneral()
        {
            try
            {
                var ingresosTotales = await _context.Ventas.SumAsync(v => (decimal?)v.Total) ?? 0m;

                var pedidosCompletados = await _context.Ventas.CountAsync();

                var clientesNew = await _context.Usuarios.CountAsync(u => u.Rol != "Administrador");

                // Separar la consulta para evitar CS8601 (asignación de referencia nula)
                var productoTopQuery = await _context.DetallesVenta
                    .Join(_context.Items,
                          detalle => detalle.IdItem,
                          item => item.Id,
                          (detalle, item) => new { item.Nombre, detalle.Cantidad })
                    .GroupBy(x => x.Nombre)
                    .OrderByDescending(g => g.Sum(x => x.Cantidad))
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                string productoTop = productoTopQuery ?? "Sin ventas";

                var productosTopList = await _context.DetallesVenta
                    .Join(_context.Items,
                          dv => dv.IdItem,
                          i => i.Id,
                          (dv, i) => new { i.Nombre, i.Juego, dv.PrecioUnitario, dv.Cantidad, dv.Subtotal })
                    .GroupBy(x => new { x.Nombre, x.Juego })
                    .Select(g => new ProductoMasVendidoDto
                    {
                        NombreCarta = g.Key.Nombre ?? string.Empty,
                        Juego = g.Key.Juego ?? string.Empty,
                        PrecioUnitario = g.Average(x => x.PrecioUnitario),
                        UnidadesVendidas = g.Sum(x => x.Cantidad),
                        IngresosTotales = g.Sum(x => x.Subtotal)
                    })
                    .OrderByDescending(x => x.UnidadesVendidas)
                    .Take(5)
                    .ToListAsync();

                var reporte = new ReporteGeneralDto
                {
                    IngresosTotales = Math.Round(ingresosTotales, 2),
                    PedidosCompletados = pedidosCompletados,
                    ClientesNew = clientesNew,
                    ProductoTop = productoTop,
                    FechaGeneracion = DateTime.Now,
                    ProductosTopList = productosTopList
                };

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al generar el reporte: {ex.Message}" });
            }
        }

        [HttpGet("ventas-por-dia")]
        async public Task<IActionResult> GetVentasPorDia()
        {
            try
            {
                var ventasRaw = await _context.Ventas
                    .Select(v => new { v.FechaVenta, v.Total })
                    .ToListAsync();

                var ventasPorDia = ventasRaw
                    .GroupBy(v => v.FechaVenta.ToString("yyyy-MM-dd"))
                    .Select(g => new {
                        Fecha = g.Key,
                        Total = g.Sum(v => v.Total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList();

                return Ok(ventasPorDia);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        [HttpGet("ventas-por-juego")]
        public async Task<IActionResult> GetVentasPorJuego()
        {
            try
            {
                var ventasPorJuego = await _context.DetallesVenta
                    .Join(_context.Items,
                          dv => dv.IdItem,
                          i => i.Id,
                          (dv, i) => new { i.Juego, dv.Subtotal })
                    .GroupBy(x => x.Juego)
                    .Select(g => new {
                        Juego = g.Key ?? "Desconocido",
                        Total = g.Sum(x => x.Subtotal)
                    })
                    .ToListAsync();

                return Ok(ventasPorJuego);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}