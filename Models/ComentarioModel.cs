using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameDeals.API.Models
{
    [Table("Comentarios")]
    public class Comentario
    {
        public int Id { get; set; }

        [Column("isDono")]
        public bool IsDono { get; set; }

        [Column("comentario_texto")]
        public string ComentarioTexto { get; set; }

        [Column("data_comentario")]
        public DateTime DataComentario { get; set; }

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Column("id_promocao")]
        public int IdPromocao { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; }

        [ForeignKey("IdPromocao")]
        public Promocao Promocao { get; set; }
    }
}
