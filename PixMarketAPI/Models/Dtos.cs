namespace PixMarketAPI.Models
{
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Rol { get; set; }
        public string? Telefono { get; set; }
        public string? Estado { get; set; }
    }

    public class LoginRequest
    {
        public string? Correo { get; set; }
        public string? Contrasenia { get; set; }
    }

    public class RegistroRequest
    {
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Contrasenia { get; set; }
        public string? Telefono { get; set; }
    }

    public class MensajeResultado
    {
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
    }

    public class TiendaResultado
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public int TotalItems { get; set; }
        public int CYgo { get; set; }
        public int CPokemon { get; set; }
        public int CMagic { get; set; }
        public int COtros { get; set; }
        public decimal PrecioMaximoReal { get; set; }
    }

    public class LineaVentaRequest
    {
        public int IdItem { get; set; }
        public int Cantidad { get; set; }
    }

    public class FinalizarVentaRequest
    {
        public int IdUsuario { get; set; }
        public List<LineaVentaRequest> Lineas { get; set; } = new List<LineaVentaRequest>();
    }

    public class FinalizarVentaResultado
    {
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
        public decimal Total { get; set; }
    }


    public class CambiarEstadoRequest
    {
        public string? Estado { get; set; }
    }

    public class InventarioDashboardDTO
    {
        public int ProductosActivos { get; set; }
        public int StockBajo { get; set; }
        public int Agotados { get; set; }
        public decimal ValorInventario { get; set; }
        public DateTime FechaActualizacion { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class ResultadoBusquedaDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Tipo { get; set; }
        public string? Url { get; set; }
    }

    public class ReporteGeneralDto
    {
        public decimal IngresosTotales { get; set; }
        public int PedidosCompletados { get; set; }
        public int ClientesNew { get; set; }
        public string ProductoTop { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }

        public List<ProductoMasVendidoDto> ProductosTopList { get; set; } = new();
    }

    public class VentaPorDiaDto
    {
        public string Fecha { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class VentaPorJuegoDto
    {
        public string Juego { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class VentaDto
    {
        public int Id { get; set; }
        public string? NombreUsuario { get; set; }
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public string? Estado { get; set; }
    }

    public class VentaDetalleDto
    {
        public int Id { get; set; }
        public string? NombreUsuario { get; set; }
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public string? Estado { get; set; }
        public List<LineaVentaDetalleDto> Lineas { get; set; } = new List<LineaVentaDetalleDto>();
    }

    public class LineaVentaDetalleDto
    {
        public string? NombreItem { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class ActualizarEstadoRequest
    {
        public string Estado { get; set; } = string.Empty;
    }

    public class ProductoMasVendidoDto
    {
        public string NombreCarta { get; set; } = string.Empty;
        public string Juego { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int UnidadesVendidas { get; set; }
        public decimal IngresosTotales { get; set; }
    }

    public class VentasStatsDto
    {
        public decimal VentasDelDia { get; set; }
        public decimal VentasDeLaSemana { get; set; }
        public int PedidosEnElMes { get; set; }
        public decimal TicketPromedio { get; set; }
    }

}