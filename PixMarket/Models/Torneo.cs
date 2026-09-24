using
    System.ComponentModel.DataAnnotations;
namespace PixMarket.Models
{
    public class Torneo
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(150)]
        public string? Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string? Juego { get; set; } = string.Empty;
        [Required]
        public DateTime Fecha { get; set; }
        [Required]
        [StringLength(20)]
        public string? Hora { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string Formato { get; set; } = string.Empty;
        [Required]
        [StringLength(30)]
        public string? Tipo { get; set; } = string.Empty;
        public int CupoMaximo { get; set; }
        [Range(0, double.MaxValue)]
        public decimal PrecioInscripcion { get; set; }
        public bool Activo { get; set; } = true;
        public ICollection<Inscripcion> Inscripcion { get; set; }
        = new List<Inscripcion>();
    }
}   