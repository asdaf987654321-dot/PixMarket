using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        private readonly IPixMarketApiService _api;
        public ReportesController(IPixMarketApiService api)
        {
            _api = api;
        }
        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}