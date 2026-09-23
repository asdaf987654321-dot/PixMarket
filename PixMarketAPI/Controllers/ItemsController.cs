using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using PixMarketAPI.Models;

namespace PixMarketAPI.Controllers
{
    [ApiController]
    [Route("api/items")]
    public class ItemsController : ControllerBase
    {
        private readonly PixContext _context;
        private readonly IWebHostEnvironment _env;

        public ItemsController(PixContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private string CarpetaImagenes()
        {
            var wwwroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var carpeta = Path.Combine(wwwroot, "Imagenes");
            Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        /// <summary>
        /// Lista todas las cartas ordenadas por juego y nombre.
        /// </summary>
        /// <returns>Un arreglo de cartas (Items).</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Item>>> Index()
        {
            var items = await _context.Items
                .OrderBy(i => i.Juego)
                .ThenBy(i => i.Nombre)
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Obtiene una carta por su identificador.
        /// </summary>
        /// <param name="id">Identificador de la carta.</param>
        /// <returns>La carta solicitada, con su información de imagen incluida.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Item), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Item>> Details(int id)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El item no existe."
                });
            }

            return Ok(item);
        }

        /// <summary>
        /// Crea una carta nueva. Envía los datos como
        /// <c>multipart/form-data</c>: el campo <c>Id</c> se ignora en el alta.
        /// </summary>
        /// <param name="item">Datos de la carta (form-data).</param>
        /// <param name="imagen">Imagen opcional (archivo).</param>
        /// <returns>La carta creada.</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Item), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Item>> Create(
            [FromForm] Item item,
            IFormFile? imagen)
        {
            if (string.IsNullOrWhiteSpace(item.Nombre) ||
                string.IsNullOrWhiteSpace(item.Juego) ||
                string.IsNullOrWhiteSpace(item.Categoria) ||
                string.IsNullOrWhiteSpace(item.Rareza) ||
                item.Precio <= 0)
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "Verifica los datos ingresados."
                });
            }

            if (await _context.Items.AnyAsync(i => i.Nombre == item.Nombre))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "Ya existe una carta con ese nombre."
                });
            }

            string? rutaImagen = null;

            if (imagen != null && imagen.Length > 0)
            {
                try
                {
                    rutaImagen = GuardarImagen(imagen);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new MensajeResultado
                    {
                        Ok = false,
                        Mensaje = ex.Message
                    });
                }
            }

            item.ImagenRuta = rutaImagen;

            try
            {
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new MensajeResultado
                    {
                        Ok = false,
                        Mensaje = "No se pudo guardar la carta. " +
                            "Verifica que la base de datos esté disponible."
                    });
            }

            return CreatedAtAction(nameof(Details), new { id = item.Id }, item);
        }

        /// <summary>
        /// Actualiza una carta existente. Envía los datos como
        /// <c>multipart/form-data</c>: el campo <c>Id</c> es obligatorio
        /// y debe coincidir con el <paramref name="id"/> de la ruta.
        /// </summary>
        /// <param name="id">Identificador de la carta a editar.</param>
        /// <param name="item">Datos de la carta (form-data).</param>
        /// <param name="imagen">Imagen opcional (archivo).</param>
        /// <returns>La carta actualizada.</returns>
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Item), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Item>> Edit(
            int id,
            [FromForm] Item item,
            IFormFile? imagen)
        {
            if (id != item.Id)
            {
                return NotFound(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El item no existe."
                });
            }

            var itemActual = await _context.Items.FindAsync(id);

            if (itemActual == null)
            {
                return NotFound(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El item no existe."
                });
            }

            if (string.IsNullOrWhiteSpace(item.Nombre) ||
                string.IsNullOrWhiteSpace(item.Juego) ||
                string.IsNullOrWhiteSpace(item.Categoria) ||
                string.IsNullOrWhiteSpace(item.Rareza) ||
                item.Precio <= 0)
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "Verifica los datos ingresados."
                });
            }

            if (await _context.Items.AnyAsync(i => i.Nombre == item.Nombre && i.Id != id))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "Ya existe otra carta con ese nombre."
                });
            }

            if (imagen != null && imagen.Length > 0)
            {
                string? rutaNueva;

                try
                {
                    rutaNueva = GuardarImagen(imagen);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new MensajeResultado
                    {
                        Ok = false,
                        Mensaje = ex.Message
                    });
                }

                EliminarImagenAnterior(itemActual.ImagenRuta);

                item.ImagenRuta = rutaNueva;
            }
            else
            {
                item.ImagenRuta = itemActual.ImagenRuta;
            }

            _context.Entry(itemActual).CurrentValues.SetValues(item);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new MensajeResultado
                    {
                        Ok = false,
                        Mensaje = "No se pudo actualizar la carta. " +
                            "Verifica que la base de datos esté disponible."
                    });
            }

            return Ok(itemActual);
        }

        /// <summary>
        /// Elimina una carta y su imagen asociada.
        /// </summary>
        /// <param name="id">Identificador de la carta a eliminar.</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El item no existe."
                });
            }

            EliminarImagenAnterior(item.ImagenRuta);

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new MensajeResultado
            {
                Ok = true,
                Mensaje = "Carta eliminada correctamente."
            });
        }

        private string GuardarImagen(IFormFile archivo)
        {
            string[] extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

            string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) ||
                !extensionesPermitidas.Contains(extension))
            {
                throw new ArgumentException("La imagen debe ser un archivo JPG, PNG o WebP.");
            }

            string nombreArchivo =
                $"item-{Guid.NewGuid():N}{extension}";

            string carpeta = CarpetaImagenes();

            string rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }

            return $"/Imagenes/{nombreArchivo}";
        }

        private void EliminarImagenAnterior(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) ||
                !ruta.StartsWith("/Imagenes/"))
            {
                return;
            }

            string nombreArchivo = Path.GetFileName(ruta);

            string rutaFisica = Path.Combine(CarpetaImagenes(), nombreArchivo);

            if (System.IO.File.Exists(rutaFisica))
            {
                System.IO.File.Delete(rutaFisica);
            }
        }
    }
}