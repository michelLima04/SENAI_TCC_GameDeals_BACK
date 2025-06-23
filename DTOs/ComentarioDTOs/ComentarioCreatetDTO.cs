namespace GameDeals.API.DTOs
{
    public class ComentarioCreateDTO
    {
        public int Id { get; set; }
        public string ComentarioTexto { get; set; }
        
        public DateTime DataComentario { get; set; }

        public string UsuarioNome { get; set; }

        public int IdPromocao { get; set; }
    }
}
