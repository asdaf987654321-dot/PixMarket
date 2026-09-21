using Microsoft.AspNetCore.Mvc;
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
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Index(string correo, string contrasenia)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo && u.Contrasenia == contrasenia);
            if (usuario != null)
            {
                if (usuario.Rol == "Administrador")
                {
                    return RedirectToAction("Administrador");
                }
                else
                {
                    return RedirectToAction("Cliente");
                }
                
            }
            ViewBag.Error = "El correo o la contraseña son incorrectos";
            
            return View();
        }

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Registro(string nombre, string correo, string contrasenia, int telefono)
        {
            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);
            if (usuarioExistente != null) 
            {
                ViewBag.Error = "El correo ya existe";
                return View();
            }
            var nuevoUsuario = new Usuario
            {
                Nombre = nombre,
                Correo = correo,
                Contrasenia = contrasenia,
                Telefono = telefono,
                Rol = "Usuario"
            };
            _context.Usuarios.Add(nuevoUsuario);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Administrador()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Cliente()
        {
            return View();
        }
    }
}
