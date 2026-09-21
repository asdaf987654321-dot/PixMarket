using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarket.Data;
using PixMarket.Models;

namespace PixMarket.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly PixContext _context;

        public UsuariosController(PixContext context)
        {
            _context = context;
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
        public IActionResult Index(
            string correo,
            string contrasenia)
        {
            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasenia))
            {
                ViewBag.Error =
                    "Debes ingresar el correo y la contraseña.";

                return View();
            }


            var usuario = _context.Usuarios
                .FirstOrDefault(u =>
                    u.Correo == correo &&
                    u.Contrasenia == contrasenia);


            if (usuario == null)
            {
                ViewBag.Error =
                    "El correo o la contraseña son incorrectos.";

                return View();
            }


            if (usuario.Rol == "Administrador")
            {
                return RedirectToAction("Administrador");
            }


            return RedirectToAction("Cliente");
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
        public IActionResult Registro(
            string nombre,
            string correo,
            string contrasenia,
            string telefono)
        {

            // ---------------------------------------------
            // VALIDACIONES
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


            // ---------------------------------------------
            // TELÉFONO
            // ---------------------------------------------

            if (!int.TryParse(telefono, out int telefonoNumero))
            {
                ViewBag.Error =
                    "El teléfono debe contener solamente números.";

                return View();
            }


            // ---------------------------------------------
            // CORREO EXISTENTE
            // ---------------------------------------------

            var usuarioExistente =
                _context.Usuarios
                    .FirstOrDefault(u =>
                        u.Correo == correo);


            if (usuarioExistente != null)
            {
                ViewBag.Error =
                    "El correo ya está registrado.";

                return View();
            }


            // ---------------------------------------------
            // CREAR USUARIO
            // ---------------------------------------------

            var nuevoUsuario = new Usuario
            {
                Nombre = nombre.Trim(),

                Correo = correo.Trim(),

                Contrasenia = contrasenia,

                Telefono = telefonoNumero,

                Rol = "Usuario"
            };


            try
            {
                _context.Usuarios.Add(nuevoUsuario);

                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ViewBag.Error =
                    "No se pudo guardar el usuario. " +
                    "Verifica que la base de datos esté disponible " +
                    "y que los datos sean válidos.";

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
        public IActionResult Administrador()
        {
            return View();
        }


        // =====================================================
        // CLIENTE
        // =====================================================

        [HttpGet]
        public IActionResult Cliente()
        {
            return View();
        }
    }
}