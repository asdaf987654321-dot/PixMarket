using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ItemsController : Controller
    {
        private readonly IPixMarketApiService _api;

        public ItemsController(IPixMarketApiService api)
        {
            _api = api;
        }

        // =====================================================
        // LISTA
        // =====================================================

        public async Task<IActionResult> Index()
        {
            List<Item> items;

            try
            {
                items = await _api.ObtenerItemsAsync();
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";

                items = new List<Item>();
            }

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

            Item? item;

            try
            {
                item = await _api.ObtenerItemAsync(id.Value);
            }
            catch (HttpRequestException)
            {
                return NotFound();
            }

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

            ApiItemResultado resultado;

            try
            {
                resultado = await _api.CrearItemAsync(item, imagen);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";
                return View(item);
            }

            if (!resultado.Ok)
            {
                ViewBag.Error =
                    string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "No se pudo guardar la carta. Verifica los datos ingresados."
                        : resultado.Mensaje;
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

            Item? item;

            try
            {
                item = await _api.ObtenerItemAsync(id.Value);
            }
            catch (HttpRequestException)
            {
                return NotFound();
            }

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

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Verifica los datos ingresados.";
                return View(item);
            }

            ApiItemResultado resultado;

            try
            {
                resultado = await _api.ActualizarItemAsync(id, item, imagen);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";
                return View(item);
            }

            if (!resultado.Ok)
            {
                ViewBag.Error =
                    string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "No se pudo actualizar la carta. Verifica los datos ingresados."
                        : resultado.Mensaje;
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

            Item? item;

            try
            {
                item = await _api.ObtenerItemAsync(id.Value);
            }
            catch (HttpRequestException)
            {
                return NotFound();
            }

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
            ApiItemResultado resultado;

            try
            {
                resultado = await _api.EliminarItemAsync(id);
            }
            catch (HttpRequestException)
            {
                resultado = new ApiItemResultado
                {
                    Ok = false,
                    Mensaje = "No se pudo conectar con la API de datos. " +
                        "Verifica que PixMarketAPI esté en ejecución."
                };
            }

            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction("Index");
            }

            TempData["Mensaje"] = "Carta eliminada correctamente.";

            return RedirectToAction("Index");
        }
    }
}