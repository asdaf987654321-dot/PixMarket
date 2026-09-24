using System.ComponentModel.DataAnnotations;

namespace PixMarket.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        [StringLength(100)]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6)]
        public string? Contrasenia { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(30)]
        public string? Rol { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(15)]
        public string? Telefono { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Activo";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public ICollection<Venta> Ventas { get; set; }
            = new List<Venta>();
    }
}