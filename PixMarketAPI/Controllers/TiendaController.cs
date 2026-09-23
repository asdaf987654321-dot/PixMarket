using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using PixMarketAPI.Models;

namespace PixMarketAPI.Controllers
{
    [ApiController]
    [Route("api/tienda")]
    public class TiendaController : ControllerBase
    {
        private readonly PixContext _context;

        public TiendaController(PixContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Devuelve las cartas de la tienda con búsqueda y filtros opcionales,
        /// junto con los contadores de las pestañas y el precio máximo real.
        /// </summary>
        /// <param name="buscar">Texto a buscar en nombre, juego, categoría o rareza.</param>
        /// <param name="juego">Juego exacto ("Pokémon", "Yu-Gi-Oh!", "Magic: The Gathering" u "Otros").</param>
        /// <param name="juegos">Lista de juegos a incluir (alternativa a <paramref name="juego"/>).</param>
        /// <param name="categorias">Lista de categorías a incluir.</param>
        /// <param name="rarezas">Lista de rarezas a incluir.</param>
        /// <param name="precioMin">Precio mínimo (decimal con punto, ej. 5.50).</param>
        /// <param name="precioMax">Precio máximo (decimal con punto, ej. 50.00).</param>
        /// <returns>Items filtrados, totales por pestaña y precio máximo.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<TiendaResultado>> Index(
            [FromQuery] string? buscar,
            [FromQuery] string? juego,
            [FromQuery] string[]? juegos,
            [FromQuery] string[]? categorias,
            [FromQuery] string[]? rarezas,
            [FromQuery] decimal? precioMin,
            [FromQuery] decimal? precioMax)
        {
            var termino = string.IsNullOrWhiteSpace(buscar)
                ? null
                : buscar.Trim();

            var query = _context.Items.AsQueryable();

            if (termino != null)
            {
                query = query.Where(i =>
                    (i.Nombre != null && i.Nombre.Contains(termino)) ||
                    (i.Juego != null && i.Juego.Contains(termino)) ||
                    (i.Categoria != null && i.Categoria.Contains(termino)) ||
                    (i.Rareza != null && i.Rareza.Contains(termino)));
            }

            var filtroJuegos = new List<string>();

            if (!string.IsNullOrWhiteSpace(juego) && juego.Trim() == "Otros")
            {
                // La pestaña "Otros" = juegos fuera de los 3 principales
                query = query.Where(i =>
                    i.Juego != "Yu-Gi-Oh!" &&
                    i.Juego != "Pokémon" &&
                    i.Juego != "Magic: The Gathering");
            }
            else if (!string.IsNullOrWhiteSpace(juego))
            {
                filtroJuegos.Add(juego.Trim());
            }
            else if (juegos != null)
            {
                filtroJuegos.AddRange(juegos
                    .Where(j => !string.IsNullOrWhiteSpace(j))
                    .Select(j => j.Trim())
                    .Distinct());
            }

            if (filtroJuegos.Any())
            {
                query = query.Where(i => i.Juego != null && filtroJuegos.Contains(i.Juego));
            }

            var filtroCategorias = (categorias ?? Array.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct()
                .ToList();

            if (filtroCategorias.Any())
            {
                query = query.Where(i => i.Categoria != null && filtroCategorias.Contains(i.Categoria));
            }

            var filtroRarezas = (rarezas ?? Array.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct()
                .ToList();

            if (filtroRarezas.Any())
            {
                query = query.Where(i => i.Rareza != null && filtroRarezas.Contains(i.Rareza));
            }

            if (precioMin.HasValue)
            {
                query = query.Where(i => i.Precio >= precioMin.Value);
            }

            if (precioMax.HasValue)
            {
                query = query.Where(i => i.Precio <= precioMax.Value);
            }

            var items = await query
                .OrderBy(i => i.Juego)
                .ThenBy(i => i.Nombre)
                .ToListAsync();

            var resultado = new TiendaResultado
            {
                Items = items,
                TotalItems = await _context.Items.CountAsync(),
                CYgo = await _context.Items.CountAsync(i => i.Juego == "Yu-Gi-Oh!"),
                CPokemon = await _context.Items.CountAsync(i => i.Juego == "Pokémon"),
                CMagic = await _context.Items.CountAsync(i => i.Juego == "Magic: The Gathering"),
                COtros = await _context.Items.CountAsync(i =>
                    i.Juego != "Yu-Gi-Oh!" &&
                    i.Juego != "Pokémon" &&
                    i.Juego != "Magic: The Gathering"),
                PrecioMaximoReal =
                    await _context.Items.MaxAsync(i => (decimal?)i.Precio) ?? 0
            };

            return Ok(resultado);
        }
    }
}