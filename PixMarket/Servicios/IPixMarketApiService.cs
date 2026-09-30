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

        Task<List<ProductoDestacadoDto>> ObtenerDestacadosAsync();

        Task<List<Item>> ObtenerItemsAsync();

        Task<Item?> ObtenerItemAsync(int id);

        Task<ApiItemResultado> CrearItemAsync(Item item, IFormFile? imagen);

        Task<ApiItemResultado> ActualizarItemAsync(int id, Item item, IFormFile? imagen);

        Task<ApiItemResultado> EliminarItemAsync(int id);

        Task<ApiLoginResultado> LoginAsync(string correo, string contrasenia);

        Task<(bool Ok, string? Mensaje)> RegistrarAsync(
            string nombre,
            string correo,
            string contrasenia,
            string telefono,
            string? rol = null);


       

        // USUARIOS (gestión)
       
        Task<List<UsuarioDto>> ObtenerUsuariosAsync();
        Task<UsuarioDto?> ObtenerUsuarioAsync(int id);
        Task<(bool Ok, string? Mensaje)> ActualizarUsuarioAsync(int id, UsuarioDto usuario);
        Task<(bool Ok, string? Mensaje)> CambiarEstadoUsuarioAsync(int id, string estado);

        // CONFIGURACIÓN
        
        Task<Configuracion> ObtenerConfiguracionAsync();

        Task<(bool Ok, string? Mensaje)> GuardarConfiguracionAsync(
            Configuracion configuracion);


        Task<ApiInventarioStats?> ObtenerEstadisticasInventarioAsync();

        Task<List<Item>?> ObtenerItemsInventarioAsync();

        Task<List<ResultadoBusquedaDto>?> BuscarGlobalAsync(string query);

        Task<ReporteGeneralDto?> GetReporteGeneralAsync();

        Task<List<VentaPorDiaDto>?> GetVentasPorDiaAsync();

        Task<List<VentaPorJuegoDto>?> GetVentasPorJuegoAsync();

        Task<List<VentaDto>?> ObtenerPedidosAdminAsync();

        Task<VentaDetalleDto?> ObtenerDetallePedidoAsync(int id);

        Task<bool> ActualizarEstadoPedidoAsync(int id, string estado);

        Task<FinalizarVentaResultado?> FinalizarVentaAsync(FinalizarVentaRequest request);

        Task<List<VentaDto>> GetVentasRecientesAsync();

        Task<VentasStatsDto?> ObtenerEstadisticasVentasAsync();


    }
}