using Microsoft.EntityFrameworkCore;
using PixMarket.Models;


namespace PixMarket.Data
{
    public class PixContext : DbContext
    {
        public PixContext(DbContextOptions<PixContext> options) : base(options)
        { }
        public DbSet<Usuario> Usuarios { get; set; }
    }
}
