using System.ComponentModel.DataAnnotations;

namespace PixMarket.Models
{
    public class Usuario
    {
        [Key]   
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string? Contrasenia { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        public string? Rol { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public int Telefono { get; set; }
    }
}
