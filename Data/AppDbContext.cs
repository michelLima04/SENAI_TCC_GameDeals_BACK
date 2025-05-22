using Microsoft.EntityFrameworkCore;
using GameDeals.API.Models;

namespace GameDeals.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Promocao> Promocoes { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }

       
        }
    }
