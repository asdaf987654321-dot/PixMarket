using
    System.ComponentModel.DataAnnotations;
namespace PixMarket.Models
{
    public class Configuracion
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string? NombreTienda { get; set; } = "Block du Booster";
        [StringLength(100)]
        public string? CorreoContacto    { get; set; }
        [StringLength(15)]
        public string? Telefono { get; set; }
        [StringLength(10)]
        public string? Moneda { get; set; } = "Bs";
        [StringLength(200)]
        public string? Direccion { get; set; }
        [StringLength(200)]
        public string? Horario { get; set; }
        public bool NotificarStockBajo { get; set; } = true;
        public bool AvisosNuevosPedidos { get; set; } = true;
        public bool TiendaPausada { get; set; } = false;

    }

}