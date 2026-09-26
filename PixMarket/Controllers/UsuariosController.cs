using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;
using System.Security.Claims;

namespace PixMarket.Controllers
{
    public class UsuariosController : Controller
    {
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


        // =====================================================
        // ADMINISTRADOR
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Administrador()
        {
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
                    telefono.Trim());
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
                return NotFound();
            }

            if (usuario == null)
            {
                return NotFound();
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
                return NotFound();
            }

            if (usuario == null)
            {
                return NotFound();
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