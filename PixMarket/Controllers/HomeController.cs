using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PixMarket.Data;

namespace PixMarket.Controllers
{
    public class HomeController : Controller
    {
        
        private readonly ILogger<HomeController> _logger;
        private readonly PixContext _context;

        public HomeController(ILogger<HomeController> logger, PixContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string correo, string contraseña)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo && u.Contrasenia == contraseña);
            if (usuario != null)
            {
                return RedirectToAction("Index");
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
        public IActionResult Registro(string nombre, string correo, string contraseña)
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
                Contrasenia = contraseña,
                Rol = "Usuario"
            };
            _context.Usuarios.Add(nuevoUsuario);
            _context.SaveChanges();
            return RedirectToAction("Login");
        }
    }
}
