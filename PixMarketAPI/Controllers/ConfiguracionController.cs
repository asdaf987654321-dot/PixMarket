using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using PixMarketAPI.Models;

namespace PixMarketAPI.Controllers
{
    [ApiController]
    [Route("api/configuracion")]
    public class ConfiguracionController : ControllerBase
    {
        private readonly PixContext _context;

        public ConfiguracionController(PixContext context)
        {
            _context = context;
        }

        // GET: api/configuracion
        [HttpGet]
        [ProducesResponseType(typeof(Configuracion), StatusCodes.Status200OK)]
        public async Task<ActionResult<Configuracion>> Obtener()
        {
            var configuracion = await _context.Configuraciones
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                configuracion = new Configuracion();
                _context.Configuraciones.Add(configuracion);
                await _context.SaveChangesAsync();
            }

            return Ok(configuracion);
        }

        // PUT: api/configuracion
        [HttpPut]
        [ProducesResponseType(typeof(MensajeResultado), StatusCodes.Status200OK)]
        public async Task<ActionResult<MensajeResultado>> Guardar(
            [FromBody] Configuracion datos)
        {
            var configuracion = await _context.Configuraciones
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                configuracion = new Configuracion();
                _context.Configuraciones.Add(configuracion);
            }

            configuracion.NombreTienda = datos.NombreTienda;
            configuracion.CorreoContacto = datos.CorreoContacto;
            configuracion.Telefono = datos.Telefono;
            configuracion.Moneda = datos.Moneda;
            configuracion.Direccion = datos.Direccion;
            configuracion.Horario = datos.Horario;
            configuracion.NotificarStockBajo = datos.NotificarStockBajo;
            configuracion.AvisosNuevosPedidos = datos.AvisosNuevosPedidos;
            configuracion.TiendaPausada = datos.TiendaPausada;

            await _context.SaveChangesAsync();

            return Ok(new MensajeResultado
            {
                Ok = true,
                Mensaje = "Configuración guardada correctamente."
            });
        }
    }
}