using Microsoft.EntityFrameworkCore;
using GameDeals.API.Models;
using GameDeals.Models;

namespace GameDeals.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Promocao> Promocoes { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }

        public DbSet<Curtidas> Curtidas{ get; set; }
        public DbSet<OperacaoLogModel> OperacaoLog { get; set; }


    }
}
