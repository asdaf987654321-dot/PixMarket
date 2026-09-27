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

        /// <summary>
        /// Hash de la contraseña (PBKDF2-SHA256), nunca la contraseña en sí.
        /// Mide unos 83 caracteres, de ahí los 200: si algún día se suben las
        /// iteraciones el hash crece y la columna sigue alcanzado.
        /// </summary>
        [Required]
        [StringLength(200)]
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
