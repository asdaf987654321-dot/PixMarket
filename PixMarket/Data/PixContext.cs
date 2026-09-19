using Microsoft.EntityFrameworkCore;
using PixMarket.Models;


namespace PixMarket.Data
{
    public class PixContext : DbContext
    {
        public PixContext(DbContextOptions<PixContext> options) : base(options)
        { }
        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<Venta> Ventas { get; set; }

        public DbSet<DetalleVenta> DetallesVenta { get; set; }

    }
}
