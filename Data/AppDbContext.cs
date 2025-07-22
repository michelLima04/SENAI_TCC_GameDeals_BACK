using Microsoft.EntityFrameworkCore;
using GameDeals.API.Models;
using GameDeals.Models;

namespace GameDeals.API.Data
{
    // Classe responsável para a Conexão com o Banco de Dados, definição das tabelas para uso do BackEnd
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
