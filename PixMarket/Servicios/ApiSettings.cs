namespace PixMarket.Servicios
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:5050/";

        // Resuelve la URL de una imagen de item:
        // - "/Imagenes/..." se sirve desde la API.
        // - Cualquier otra ruta se usa tal cual (imagen local de la app).
        public string UrlImagen(string? ruta, string fallback = "~/Imagenes/imagen1.jpg")
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return fallback;
            }

            if (ruta.StartsWith("/Imagenes/"))
            {
                return BaseUrl.TrimEnd('/') + ruta;
            }

            return ruta;
        }
    }
}