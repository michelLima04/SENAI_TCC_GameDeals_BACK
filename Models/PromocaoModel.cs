using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameDeals.API.Models
{
    public class Promocao
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(1000)]
        public string? Url { get; set; }

        [MaxLength(20)]
        public string? Cupom { get; set; }

        [Required]
        [MaxLength(255)]
        public string Site { get; set; }

        [Required]
        [MaxLength(100)]
        public string Titulo { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Preco { get; set; }

        [MaxLength(1000)]
        [Column("imagem_url")]
        public string? ImagemUrl { get; set; }

        [Column("tempo_postado")]
        public TimeSpan? TempoPostado { get; set; }

        [Column("status_publicacao")]
        public bool StatusPublicacao { get; set; } = false;

        [Column("motivo_inativacao")]
        public string? MotivoInativacao { get; set; }

        [ForeignKey("Usuario")]
        [Column("id_usuario")]
        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public List<Comentario> Comentarios { get; set; } = new();


    }
}
