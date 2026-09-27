using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data; // Asegúrate de que coincida con tu namespace de contexto de BD
using PixMarketAPI.Models; // Asegúrate de que coincida con tu namespace de modelos
using System;
using System.Threading.Tasks;

namespace PixMarketAPI.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    public class InventarioController : ControllerBase
    {
        private readonly PixContext _context; // Reemplaza 'ApplicationDbContext' por el nombre de tu contexto de BD

        public InventarioController(PixContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<InventarioDashboardDTO>> GetDashboardStats()
        {
            try
            {
                // Consultas usando EF Core basadas en tu modelo de inventario/ítems
                // (Cambia '_context.Items' por el nombre de tu DbSet, ej: _context.Productos)
                var productosActivos = await _context.Items.CountAsync(p => p.Stock > 0);

                var stockBajo = await _context.Items.CountAsync(p => p.Stock > 0 && p.Stock <= 5);

                var agotados = await _context.Items.CountAsync(p => p.Stock == 0);

                var valorInventario = await _context.Items.SumAsync(p => (decimal?)(p.Stock * p.Precio)) ?? 0;

                var resultado = new InventarioDashboardDTO
                {
                    ProductosActivos = productosActivos,
                    StockBajo = stockBajo,
                    Agotados = agotados,
                    ValorInventario = Math.Round(valorInventario, 2),
                    FechaActualizacion = DateTime.Now
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new MensajeResultado
                {
                    Ok = false,
                    Mensaje = $"Error al obtener las estadísticas: {ex.Message}"
                });
            }

        }

        [HttpGet("items")]
        public async Task<ActionResult<List<Item>>> GetInventarioItems()
        {
            try
            {
                var items = await _context.Items.ToListAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}