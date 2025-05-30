using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameDeals.API.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string NomeSobrenome { get; set; }

        [Required]
        public string UsuarioNome { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Senha { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        public int Contribuicoes { get; set; } = 0;

        public bool IsAdmin { get; set; }
        public bool EstaBloqueado { get; set; } = false;
    }
}
