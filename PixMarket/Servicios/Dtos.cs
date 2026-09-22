using PixMarket.Models;

namespace PixMarket.Servicios
{
    public class TiendaResultadoDto
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public int TotalItems { get; set; }
        public int CYgo { get; set; }
        public int CPokemon { get; set; }
        public int CMagic { get; set; }
        public int COtros { get; set; }
        public decimal PrecioMaximoReal { get; set; }
    }

    public class MensajeDto
    {
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
    }

    public class FinalizarVentaDto
    {
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
        public decimal Total { get; set; }
    }

    public class UsuarioDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Rol { get; set; }
    }

    public class ApiItemResultado
    {
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
        public Item? Item { get; set; }
    }

    public class ApiLoginResultado
    {
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
        public UsuarioDto? Usuario { get; set; }
    }
}