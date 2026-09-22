using Microsoft.AspNetCore.WebUtilities;
using PixMarket.Models;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PixMarket.Servicios
{
    public class PixMarketApiService : IPixMarketApiService
    {
        private readonly HttpClient _http;

        public PixMarketApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<TiendaResultadoDto> ObtenerTiendaAsync(
            string? buscar,
            string? juego,
            string[]? juegos,
            string[]? categorias,
            string[]? rarezas,
            decimal? precioMin,
            decimal? precioMax)
        {
            var parametros = new List<KeyValuePair<string, string?>>();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                parametros.Add(new("buscar", buscar.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(juego))
            {
                parametros.Add(new("juego", juego.Trim()));
            }

            foreach (var valor in juegos ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    parametros.Add(new("juegos", valor.Trim()));
                }
            }

            foreach (var valor in categorias ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    parametros.Add(new("categorias", valor.Trim()));
                }
            }

            foreach (var valor in rarezas ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    parametros.Add(new("rarezas", valor.Trim()));
                }
            }

            if (precioMin.HasValue)
            {
                parametros.Add(new("precioMin", precioMin.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (precioMax.HasValue)
            {
                parametros.Add(new("precioMax", precioMax.Value.ToString(CultureInfo.InvariantCulture)));
            }

            var url = QueryHelpers.AddQueryString("api/tienda", parametros);

            return await _http.GetFromJsonAsync<TiendaResultadoDto>(url)
                ?? new TiendaResultadoDto();
        }

        public async Task<List<Item>> ObtenerItemsAsync()
        {
            return await _http.GetFromJsonAsync<List<Item>>("api/items")
                ?? new List<Item>();
        }

        public async Task<Item?> ObtenerItemAsync(int id)
        {
            var respuesta = await _http.GetAsync($"api/items/{id}");

            if (!respuesta.IsSuccessStatusCode)
            {
                return null;
            }

            return await respuesta.Content.ReadFromJsonAsync<Item>();
        }

        public async Task<ApiItemResultado> CrearItemAsync(Item item, IFormFile? imagen)
        {
            using var contenido = ContenidoItem(item, imagen);

            var respuesta = await _http.PostAsync("api/items", contenido);

            if (respuesta.StatusCode == HttpStatusCode.Created)
            {
                var creado = await respuesta.Content.ReadFromJsonAsync<Item>();

                return new ApiItemResultado { Ok = true, Item = creado };
            }

            return new ApiItemResultado
            {
                Ok = false,
                Mensaje = await LeerMensaje(respuesta)
            };
        }

        public async Task<ApiItemResultado> ActualizarItemAsync(int id, Item item, IFormFile? imagen)
        {
            using var contenido = ContenidoItem(item, imagen);

            var respuesta = await _http.PutAsync($"api/items/{id}", contenido);

            if (respuesta.StatusCode == HttpStatusCode.OK)
            {
                var actualizado = await respuesta.Content.ReadFromJsonAsync<Item>();

                return new ApiItemResultado { Ok = true, Item = actualizado };
            }

            return new ApiItemResultado
            {
                Ok = false,
                Mensaje = await LeerMensaje(respuesta)
            };
        }

        public async Task<ApiItemResultado> EliminarItemAsync(int id)
        {
            var respuesta = await _http.DeleteAsync($"api/items/{id}");

            if (respuesta.StatusCode == HttpStatusCode.OK)
            {
                return new ApiItemResultado { Ok = true };
            }

            return new ApiItemResultado
            {
                Ok = false,
                Mensaje = await LeerMensaje(respuesta)
            };
        }

        public async Task<ApiLoginResultado> LoginAsync(string correo, string contrasenia)
        {
            var respuesta = await _http.PostAsJsonAsync("api/usuarios/login",
                new { correo, contrasenia });

            if (respuesta.StatusCode == HttpStatusCode.OK)
            {
                var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioDto>();

                return new ApiLoginResultado
                {
                    Ok = true,
                    Usuario = usuario
                };
            }

            return new ApiLoginResultado
            {
                Ok = false,
                Mensaje = await LeerMensaje(respuesta)
            };
        }

        public async Task<(bool Ok, string? Mensaje)> RegistrarAsync(
            string nombre, string correo, string contrasenia, string telefono)
        {
            var respuesta = await _http.PostAsJsonAsync("api/usuarios/registro",
                new { nombre, correo, contrasenia, telefono });

            if (respuesta.StatusCode == HttpStatusCode.OK)
            {
                return (true, null);
            }

            return (false, await LeerMensaje(respuesta));
        }

        public async Task<FinalizarVentaDto> FinalizarVentaAsync(List<ItemCarrito> lineas)
        {
            var request = new
            {
                lineas = lineas.Select(l => new
                {
                    idItem = l.Id,
                    cantidad = l.Cantidad
                }).ToList()
            };

            var respuesta = await _http.PostAsJsonAsync("api/ventas/finalizar", request);

            if (respuesta.StatusCode == HttpStatusCode.OK)
            {
                return await respuesta.Content.ReadFromJsonAsync<FinalizarVentaDto>()
                    ?? new FinalizarVentaDto { Ok = true };
            }

            if (respuesta.StatusCode == HttpStatusCode.Conflict ||
                respuesta.StatusCode == HttpStatusCode.BadRequest)
            {
                var resultado = await respuesta.Content.ReadFromJsonAsync<FinalizarVentaDto>();

                return resultado ?? new FinalizarVentaDto { Ok = false };
            }

            return new FinalizarVentaDto
            {
                Ok = false,
                Mensaje = await LeerMensaje(respuesta)
            };
        }

        private static MultipartFormDataContent ContenidoItem(Item item, IFormFile? imagen)
        {
            var contenido = new MultipartFormDataContent();

            contenido.Add(new StringContent(item.Id.ToString(CultureInfo.InvariantCulture)), "Id");
            contenido.Add(new StringContent(item.Nombre ?? ""), "Nombre");
            contenido.Add(new StringContent(item.Descripcion ?? ""), "Descripcion");
            contenido.Add(new StringContent(item.Juego ?? ""), "Juego");
            contenido.Add(new StringContent(item.Categoria ?? ""), "Categoria");
            contenido.Add(new StringContent(item.Rareza ?? ""), "Rareza");
            contenido.Add(new StringContent(
                item.Precio.ToString(CultureInfo.InvariantCulture)), "Precio");
            contenido.Add(new StringContent(
                item.Stock.ToString(CultureInfo.InvariantCulture)), "Stock");

            if (imagen != null && imagen.Length > 0)
            {
                Stream archivo = new MemoryStream();
                imagen.CopyTo(archivo);
                archivo.Position = 0;

                var contenidoArchivo = new StreamContent(archivo);
                contenidoArchivo.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                contenido.Add(contenidoArchivo, "imagen", imagen.FileName);
            }

            return contenido;
        }

        private static async Task<string?> LeerMensaje(HttpResponseMessage respuesta)
        {
            try
            {
                var mensaje = await respuesta.Content.ReadFromJsonAsync<MensajeDto>();
                return mensaje?.Mensaje;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}