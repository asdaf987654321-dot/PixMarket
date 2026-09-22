namespace PixMarketAPI.Models
{
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Rol { get; set; }
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
        public List<LineaVentaRequest> Lineas { get; set; } = new List<LineaVentaRequest>();
    }

    public class FinalizarVentaResultado
    {
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
        public decimal Total { get; set; }
    }
}