using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarket.Data;
using PixMarket.Models;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ItemsController : Controller
    {
        private readonly PixContext _context;
        private readonly IWebHostEnvironment _env;

        public ItemsController(PixContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =====================================================
        // LISTA
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var items = await _context.Items
                .OrderBy(i => i.Juego)
                .ThenBy(i => i.Nombre)
                .ToListAsync();

            return View(items);
        }

        // =====================================================
        // DETALLES
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // =====================================================
        // CREAR
        // =====================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Descripcion,Juego,Categoria,Rareza,Precio,Stock")] Item item,
            IFormFile? imagen)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Verifica los datos ingresados.";
                return View(item);
            }

            if (await _context.Items.AnyAsync(i => i.Nombre == item.Nombre))
            {
                ViewBag.Error = "Ya existe una carta con ese nombre.";
                return View(item);
            }

            string? rutaImagen = null;

            if (imagen != null && imagen.Length > 0)
            {
                rutaImagen = GuardarImagen(imagen);

                if (rutaImagen == null)
                {
                    ViewBag.Error = "La imagen debe ser un archivo JPG, PNG o WebP.";
                    return View(item);
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
                ViewBag.Error =
                    "No se pudo guardar la carta. " +
                    "Verifica que la base de datos esté disponible.";
                return View(item);
            }

            TempData["Mensaje"] = "Carta creada correctamente.";

            return RedirectToAction("Index");
        }

        // =====================================================
        // EDITAR
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Nombre,Descripcion,Juego,Categoria,Rareza,Precio,Stock")] Item item,
            IFormFile? imagen)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            var itemActual = await _context.Items.FindAsync(id);

            if (itemActual == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Verifica los datos ingresados.";
                item.ImagenRuta = itemActual.ImagenRuta;
                return View(item);
            }

            if (await _context.Items.AnyAsync(i => i.Nombre == item.Nombre && i.Id != id))
            {
                ViewBag.Error = "Ya existe otra carta con ese nombre.";
                item.ImagenRuta = itemActual.ImagenRuta;
                return View(item);
            }

            if (imagen != null && imagen.Length > 0)
            {
                string? rutaNueva = GuardarImagen(imagen);

                if (rutaNueva == null)
                {
                    ViewBag.Error = "La imagen debe ser un archivo JPG, PNG o WebP.";
                    item.ImagenRuta = itemActual.ImagenRuta;
                    return View(item);
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
                ViewBag.Error =
                    "No se pudo actualizar la carta. " +
                    "Verifica que la base de datos esté disponible.";
                return View(item);
            }

            TempData["Mensaje"] = "Carta actualizada correctamente.";

            return RedirectToAction("Index");
        }

        // =====================================================
        // ELIMINAR
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return RedirectToAction("Index");
            }

            EliminarImagenAnterior(item.ImagenRuta);

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Carta eliminada correctamente.";

            return RedirectToAction("Index");
        }

        // =====================================================
        // IMÁGENES
        // =====================================================

        private string? GuardarImagen(IFormFile archivo)
        {
            string[] extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

            string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) ||
                !extensionesPermitidas.Contains(extension))
            {
                return null;
            }

            string nombreArchivo =
                $"item-{Guid.NewGuid():N}{extension}";

            string carpeta = Path.Combine(_env.WebRootPath, "Imagenes");

            Directory.CreateDirectory(carpeta);

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

            string rutaFisica = Path.Combine(_env.WebRootPath, "Imagenes", nombreArchivo);

            if (System.IO.File.Exists(rutaFisica))
            {
                System.IO.File.Delete(rutaFisica);
            }
        }
    }
}