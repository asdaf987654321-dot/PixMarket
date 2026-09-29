using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;
using System.Security.Claims;
using PixMarket.Models;

namespace PixMarket.Controllers
{
    public class UsuariosController : Controller
    {
        private const string MessageApiCaida =
            "No se pudo obtener el usuario. " +
            "Verifica que PixMarketAPI esté en ejecución y que la base de datos " +
            "tenga el esquema actualizado.";

        private readonly IPixMarketApiService _api;

        public UsuariosController(IPixMarketApiService api)
        {
            _api = api;
        }


        // =====================================================
        // LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        // =====================================================
        // LOGIN - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            string correo,
            string contrasenia,
            bool recordarme)
        {
            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasenia))
            {
                ViewBag.Error =
                    "Debes ingresar el correo y la contraseña.";

                return View();
            }

            ApiLoginResultado resultado;

            try
            {
                resultado = await _api.LoginAsync(correo.Trim(), contrasenia);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";

                return View();
            }

            if (!resultado.Ok || resultado.Usuario == null)
            {
                ViewBag.Error =
                    string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "El correo o la contraseña son incorrectos."
                        : resultado.Mensaje;

                return View();
            }

            var usuario = resultado.Usuario;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre ?? usuario.Correo ?? ""),
                new Claim(ClaimTypes.Email, usuario.Correo ?? ""),
                new Claim(ClaimTypes.Role, usuario.Rol ?? "")
            };

            var identity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var propiedades = new AuthenticationProperties();

            if (recordarme)
            {
                propiedades.IsPersistent = true;
                propiedades.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                propiedades);


            if (usuario.Rol == "Administrador")
            {
                return RedirectToAction("Administrador");
            }


            return RedirectToAction("Cliente");
        }


        // =====================================================
        // LOGOUT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }


        // =====================================================
        // REGISTRO - GET
        // =====================================================

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }


        // =====================================================
        // REGISTRO - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(
            string nombre,
            string correo,
            string contrasenia,
            string telefono)
        {

            // ---------------------------------------------
            // VALIDACIONES LOCALES
            // ---------------------------------------------

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ViewBag.Error =
                    "El nombre es obligatorio.";

                return View();
            }


            if (string.IsNullOrWhiteSpace(correo))
            {
                ViewBag.Error =
                    "El correo es obligatorio.";

                return View();
            }


            if (string.IsNullOrWhiteSpace(contrasenia))
            {
                ViewBag.Error =
                    "La contraseña es obligatoria.";

                return View();
            }


            if (contrasenia.Length < 6)
            {
                ViewBag.Error =
                    "La contraseña debe tener mínimo 6 caracteres.";

                return View();
            }


            if (!int.TryParse(telefono, out _))
            {
                ViewBag.Error =
                    "El teléfono debe contener solamente números.";

                return View();
            }


            // ---------------------------------------------
            // LLAMAR A LA API
            // ---------------------------------------------

            (bool Ok, string? Mensaje) resultado;

            try
            {
                resultado = await _api.RegistrarAsync(
                    nombre.Trim(), correo.Trim(), contrasenia, telefono);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";

                return View();
            }


            if (!resultado.Ok)
            {
                ViewBag.Error =
                    string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "No se pudo guardar el usuario. Verifica los datos ingresados."
                        : resultado.Mensaje;

                return View();
            }


            // ---------------------------------------------
            // REGISTRO CORRECTO
            // ---------------------------------------------

            TempData["RegistroExitoso"] =
                "Cuenta creada correctamente.";

            return RedirectToAction("Index");
        }
        // ADMINISTRADOR
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Administrador()
        {
            // Obtenemos los datos de la API en paralelo para que sea más rápido
            ApiInventarioStats? inventario = null;
            VentasStatsDto? ventas = null;
            List<VentaDto>? pedidos = null;
            List<UsuarioDto>? usuarios = null;

            try
            {
                // Ejecutamos todas las llamadas en paralelo
                var tareaInventario = _api.ObtenerEstadisticasInventarioAsync();
                var tareaVentas = _api.ObtenerEstadisticasVentasAsync();
                var tareaPedidos = _api.ObtenerPedidosAdminAsync();
                var tareaUsuarios = _api.ObtenerUsuariosAsync();

                await Task.WhenAll(tareaInventario, tareaVentas, tareaPedidos, tareaUsuarios);

                inventario = tareaInventario.Result;
                ventas = tareaVentas.Result;
                pedidos = tareaPedidos.Result;
                usuarios = tareaUsuarios.Result;
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";
            }

            // Pasamos los datos a la vista usando ViewBag
            ViewBag.TotalProductos = inventario?.ProductosActivos ?? 0;
            ViewBag.VentasDelDia = ventas?.VentasDelDia ?? 0m;
            ViewBag.PedidosPendientes = pedidos?.Count(p => p.Estado == "Pendiente") ?? 0;
            ViewBag.UsuariosRegistrados = usuarios?.Count ?? 0;

            // Datos para el gráfico de ventas (últimos 7 días)
            // Por ahora usamos datos de la API de ventas por día
            ViewBag.VentasPorDia = new List<VentaPorDiaDto>();

            try
            {
                var ventasPorDia = await _api.GetVentasPorDiaAsync();
                if (ventasPorDia != null)
                {
                    ViewBag.VentasPorDia = ventasPorDia;
                }
            }
            catch (HttpRequestException) { /* Silencioso: no es crítico */ }

            // Stock bajo: productos con stock <= 5
            List<Item> stockBajo = new();
            try
            {
                var items = await _api.ObtenerItemsAsync();
                stockBajo = items
                    .Where(i => i.Stock > 0 && i.Stock <= 5)
                    .OrderBy(i => i.Stock)
                    .Take(5)
                    .ToList();
            }
            catch (HttpRequestException) { /* Silencioso */ }

            ViewBag.StockBajo = stockBajo;

            // Pedidos recientes: los últimos 5
            ViewBag.PedidosRecientes = (pedidos ?? new List<VentaDto>())
                .OrderByDescending(p => p.FechaVenta)
                .Take(5)
                .ToList();

            return View();
        }





        // GESTIÓN DE USUARIOS (PANEL ADMIN)
        // Lista de usuarios
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Gestion()
        {
            List<UsuarioDto> usuarios;

            try
            {
                usuarios = await _api.ObtenerUsuariosAsync();
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "No se pudo conectar con la API de datos. " +
                    "Verifica que PixMarketAPI esté en ejecución.";

                usuarios = new List<UsuarioDto>();
            }

            return View(usuarios);
        }

        // Crear nuevo usuario (desde el panel admin)
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Crear(
            string nombre,
            string correo,
            string contrasenia,
            string telefono,
            string rol)
        {
            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasenia) ||
                string.IsNullOrWhiteSpace(telefono) ||
                string.IsNullOrWhiteSpace(rol))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                return View();
            }

            (bool Ok, string? Mensaje) resultado;

            try
            {
                resultado = await _api.RegistrarAsync(
                    nombre.Trim(),
                    correo.Trim(),
                    contrasenia,
                    telefono.Trim(),
                    rol);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "No se pudo conectar con la API de datos.";
                return View();
            }

            if (!resultado.Ok)
            {
                ViewBag.Error = resultado.Mensaje ?? "No se pudo crear el usuario.";
                return View();
            }

            TempData["Mensaje"] = "Usuario creado correctamente.";
            return RedirectToAction("Gestion");
        }

        // Ver detalle de un usuario
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            UsuarioDto? usuario;

            try
            {
                usuario = await _api.ObtenerUsuarioAsync(id.Value);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = MessageApiCaida;
                return RedirectToAction("Gestion");
            }

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction("Gestion");
            }

            return View(usuario);
        }

        // Editar usuario
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            UsuarioDto? usuario;

            try
            {
                usuario = await _api.ObtenerUsuarioAsync(id.Value);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = MessageApiCaida;
                return RedirectToAction("Gestion");
            }

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction("Gestion");
            }

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, UsuarioDto usuario)
        {
            if (id != usuario.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(usuario.Nombre))
            {
                ViewBag.Error = "El nombre es obligatorio.";
                return View(usuario);
            }

            if (string.IsNullOrWhiteSpace(usuario.Correo))
            {
                ViewBag.Error = "El correo es obligatorio.";
                return View(usuario);
            }

            if (string.IsNullOrWhiteSpace(usuario.Rol))
            {
                usuario.Rol = "Usuario";
            }

            // El estado solo cambia con la acción "Suspender / Activar".
            usuario.Estado = null;

            (bool Ok, string? Mensaje) resultado;

            try
            {
                resultado = await _api.ActualizarUsuarioAsync(id, usuario);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "No se pudo conectar con la API de datos.";
                return View(usuario);
            }

            if (!resultado.Ok)
            {
                ViewBag.Error = resultado.Mensaje ?? "No se pudo actualizar el usuario.";
                return View(usuario);
            }

            TempData["Mensaje"] = "Usuario actualizado correctamente.";
            return RedirectToAction("Gestion");
        }

        // Suspender / Activar usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            (bool Ok, string? Mensaje) resultado;

            try
            {
                resultado = await _api.CambiarEstadoUsuarioAsync(id, estado);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con la API de datos.";
                return RedirectToAction("Gestion");
            }

            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Mensaje ?? "No se pudo cambiar el estado.";
                return RedirectToAction("Gestion");
            }

            TempData["Mensaje"] = $"Usuario {(estado == "Activo" ? "activado" : "suspendido")} correctamente.";
            return RedirectToAction("Gestion");
        }








        // =====================================================
        // CLIENTE
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "Usuario")]
        public IActionResult Cliente()
        {
            return View();
        }
    }
}