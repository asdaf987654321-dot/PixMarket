using System.ComponentModel;
using 
    System.ComponentModel.DataAnnotations;
namespace PixMarket.Models
{
    public class MensajeContacto
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string? Nombre { get; set; }= string.Empty;
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;
        [Required]
        [StringLength(150)]
        public string? Asunto { get; set; } = string.Empty;
        [Required]
        [StringLength(2000)]
        public string? Mensaje { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
    }
}