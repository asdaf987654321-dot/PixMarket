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