using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameDeals.API.Models;

namespace GameDeals.Models
{
    public class Curtidas
    {
        [Key]
        public int id { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("id_promocao")]
        public int id_promocao { get; set; }

        [ForeignKey("id_promocao")]
        public Promocao Promocao { get; set; }

        [Column("id_usuario")]
        public int id_usuario { get; set; }

        [ForeignKey("id_usuario")]
        public Usuario Usuario { get; set; }
    }
}