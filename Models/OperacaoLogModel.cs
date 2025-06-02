using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameDeals.API.Models
{
    [Table("OperacoesLog")]
    public class OperacaoLogModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; }

        [Required]
        [Column("acao")]
        [MaxLength(100)]
        public string Acao { get; set; }

        [Column("entidade_afetada")]
        [MaxLength(100)]
        public string? EntidadeAfetada { get; set; }

        [Column("id_entidade")]
        public int? IdEntidade { get; set; }

        [Column("detalhes")]
        public string? Detalhes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
