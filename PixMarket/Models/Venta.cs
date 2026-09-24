using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PixMarket.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }

        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Required (ErrorMessage = "El total es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El total debe ser mayor que cero")]
        public decimal Total { get; set; }

        [StringLength(100)]
        public string? MetodoPago { get; set; }
        public int? UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; }
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";
        public ICollection<DetalleVenta> DetallesVenta { get; set; }
            = new List<DetalleVenta>();
    }
}
