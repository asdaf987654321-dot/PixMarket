using System.ComponentModel.DataAnnotations;

namespace PixMarketAPI.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Nombre { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string? Correo { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string? Contrasenia { get; set; }

        [Required]
        [StringLength(30)]
        public string? Rol { get; set; }

        [Required]
        [StringLength(15)]
        public string? Telefono { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Activo";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public ICollection<Venta> Ventas { get; set; }
            = new List<Venta>();
    }
}
