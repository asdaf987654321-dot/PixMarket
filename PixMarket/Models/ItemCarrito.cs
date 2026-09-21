namespace PixMarket.Models
{
    public class ItemCarrito
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }

        public string? Juego { get; set; }

        public string? Categoria { get; set; }

        public string? Rareza { get; set; }

        public decimal Precio { get; set; }

        public string? ImagenRuta { get; set; }

        public int Cantidad { get; set; }

        public int Stock { get; set; }

        public decimal Subtotal => Precio * Cantidad;
    }
}