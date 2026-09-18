using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PixMarket.Models
{
    public class DetalleVenta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdVenta { get; set; }

        [Required]
        public int IdItem { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El Precio unitario debe ser mayor que cero")]
        public decimal PrecioUnitario { get; set; }

        [Required(ErrorMessage = "El subtotal es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El Subtotal debe ser mayor que cero")]
        public decimal Subtotal { get; set; }

        [ForeignKey(nameof(IdVenta))]
        public Venta Venta { get; set; } = null!;

        [ForeignKey(nameof(IdItem))]
        public Item Item { get; set; } = null!;
    }
}
