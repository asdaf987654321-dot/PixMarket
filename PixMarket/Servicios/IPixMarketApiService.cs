using PixMarket.Models;

namespace PixMarket.Servicios
{
    public interface IPixMarketApiService
    {
        Task<TiendaResultadoDto> ObtenerTiendaAsync(
            string? buscar,
            string? juego,
            string[]? juegos,
            string[]? categorias,
            string[]? rarezas,
            decimal? precioMin,
            decimal? precioMax);

        Task<List<Item>> ObtenerItemsAsync();

        Task<Item?> ObtenerItemAsync(int id);

        Task<ApiItemResultado> CrearItemAsync(Item item, IFormFile? imagen);

        Task<ApiItemResultado> ActualizarItemAsync(int id, Item item, IFormFile? imagen);

        Task<ApiItemResultado> EliminarItemAsync(int id);

        Task<ApiLoginResultado> LoginAsync(string correo, string contrasenia);

        Task<(bool Ok, string? Mensaje)> RegistrarAsync(
            string nombre, string correo, string contrasenia, string telefono);

        Task<FinalizarVentaDto> FinalizarVentaAsync(List<ItemCarrito> lineas);
    }
}