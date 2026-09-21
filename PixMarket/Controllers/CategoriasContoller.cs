using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarket.Data;

namespace PixMarket.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly PixContext _context;

        public CategoriasController(PixContext context)
        {
            _context = context;
        }

        // Vista de TIENDA
        public async Task<IActionResult> Index(
            string? buscar,
            string? juego,
            string[]? juegos,
            string[]? categorias,
            string[]? rarezas,
            decimal? precioMin,
            decimal? precioMax)
        {
            var termino = string.IsNullOrWhiteSpace(buscar)
                ? null
                : buscar.Trim();

            var query = _context.Items.AsQueryable();

            // BUSCAR (texto)
            if (termino != null)
            {
                query = query.Where(i =>
                    (i.Nombre != null && i.Nombre.Contains(termino)) ||
                    (i.Juego != null && i.Juego.Contains(termino)) ||
                    (i.Categoria != null && i.Categoria.Contains(termino)) ||
                    (i.Rareza != null && i.Rareza.Contains(termino)));
            }

            // JUEGO (pestaña o checkboxes)
            var filtroJuegos = new List<string>();

            if (!string.IsNullOrWhiteSpace(juego) && juego.Trim() == "Otros")
            {
                // La pestaña "Otros" = juegos fuera de los 3 principales
                query = query.Where(i =>
                    i.Juego != "Yu-Gi-Oh!" &&
                    i.Juego != "Pokémon" &&
                    i.Juego != "Magic: The Gathering");

                filtroJuegos.Add("Otros");
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

            // CATEGORÍA
            var filtroCategorias = (categorias ?? Array.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct()
                .ToList();

            if (filtroCategorias.Any())
            {
                query = query.Where(i => i.Categoria != null && filtroCategorias.Contains(i.Categoria));
            }

            // RAREZA
            var filtroRarezas = (rarezas ?? Array.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct()
                .ToList();

            if (filtroRarezas.Any())
            {
                query = query.Where(i => i.Rareza != null && filtroRarezas.Contains(i.Rareza));
            }

            // PRECIO
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

            // Estado de los filtros (para que queden marcados)
            ViewBag.Buscar = termino ?? "";
            ViewBag.Juego = string.IsNullOrWhiteSpace(juego) ? "" : juego.Trim();
            ViewBag.SelJuegos = filtroJuegos;
            ViewBag.SelCategorias = filtroCategorias;
            ViewBag.SelRarezas = filtroRarezas;
            ViewBag.PrecioMin = precioMin;
            ViewBag.PrecioMax = precioMax;
            ViewBag.PrecioMaximoReal =
                await _context.Items.MaxAsync(i => (decimal?)i.Precio) ?? 0;

            // Contadores de pestañas (sobre el total de la tienda)
            ViewBag.TotalItems = await _context.Items.CountAsync();
            ViewBag.CYgo = await _context.Items.CountAsync(i => i.Juego == "Yu-Gi-Oh!");
            ViewBag.CPokemon = await _context.Items.CountAsync(i => i.Juego == "Pokémon");
            ViewBag.CMagic = await _context.Items.CountAsync(i => i.Juego == "Magic: The Gathering");
            ViewBag.COtros = await _context.Items.CountAsync(i =>
                i.Juego != "Yu-Gi-Oh!" &&
                i.Juego != "Pokémon" &&
                i.Juego != "Magic: The Gathering");

            return View(items);
        }

        // Vista de CATEGORÍAS
        public IActionResult Categorias()
        {
            return View();
        }
    }
}