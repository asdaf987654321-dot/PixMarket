using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Servicios;


using PixMarket.Controllers; 

namespace PixMarket.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {

        private readonly IPixMarketApiService _api;

        private readonly IConverter _converter;

        public ReportesController(IPixMarketApiService api, IConverter converter)

        {
            _api = api;
            _converter = converter;
        }

        public async Task<IActionResult> Index()
        {
            var reporte = await _api.GetReporteGeneralAsync();
            ViewBag.VentasDia = await _api.GetVentasPorDiaAsync();
            ViewBag.VentasJuego = await _api.GetVentasPorJuegoAsync();
            return View(reporte);
        }


        [HttpGet]
        public async Task<IActionResult> DescargarPdfReporte()
        {
            var modelo = await _api.GetReporteGeneralAsync();

            // Renderizamos la nueva vista dedicada para PDF
            string htmlContent = await this.RenderViewToStringAsync("ReportePdf", modelo);

            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                DocumentTitle = "Reporte_PixMarket"
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = htmlContent,
                HeaderSettings = { FontName = "Arial", FontSize = 9, Right = "Página [page] de [toPage]", Line = true },
                FooterSettings = { FontName = "Arial", FontSize = 9, Center = "PixMarket - Reporte Oficial", Line = true }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            byte[] file = _converter.Convert(pdf);
            return File(file, "application/pdf", $"Reporte_PixMarket_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string q)
        {
            var resultados = await _api.BuscarGlobalAsync(q);
            ViewBag.Query = q;
            return View(resultados);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarJson(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new List<object>());
            }

            var resultados = await _api.BuscarGlobalAsync(q);
            return Json(resultados);
        }
    }
}