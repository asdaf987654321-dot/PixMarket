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

    }
}
