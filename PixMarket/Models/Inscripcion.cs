using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PixMarket.Models
{
    public class Inscripcion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdTorneo { get; set; }

        [ForeignKey(nameof(IdTorneo))]
        public Torneo Torneo { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Juego { get; set; }

        [StringLength(500)]
        public string? Comentario { get; set; }

        public DateTime FechaInscripcion { get; set; } = DateTime.Now;
    }
}