using System;

namespace GameDeals.API.DTOs
{
    public class ComentarioResponseDTO
    {
        public int Id { get; set; }
        public string ComentarioTexto { get; set; }
        public DateTime DataComentario { get; set; }
        public bool IsDono { get; set; }

        public int IdUsuario { get; set; }
        public string UsuarioNome { get; set; }

        public int IdPromocao { get; set; }
    }
}
