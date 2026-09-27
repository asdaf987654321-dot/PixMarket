using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.IO;
using System.Threading.Tasks;

namespace PixMarket.Controllers
{
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewToStringAsync<TModel>(this Controller controller, string viewName, TModel model)
        {
            controller.ViewData.Model = model;
            using (var writer = new StringWriter())
            {
                var viewEngine = controller.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
                var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, false);

                if (!viewResult.Success)
                {
                    throw new FileNotFoundException($"No se pudo encontrar la vista {viewName}");
                }

                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.ToString();
            }
        }
    }
}