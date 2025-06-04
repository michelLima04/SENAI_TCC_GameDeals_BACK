using static AppPromocoesGamer.API.Controllers.PromocaoController;

namespace GameDeals.DTOs.PromocaoDTOs
{
    public class PromocaoFeedDTO
    {
        public int Id { get; set; }

        public string? Url { get; set; }
        public string Titulo { get; set; }
        public string Site { get; set; }
        public decimal Preco { get; set; }
        public string? ImagemUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UsuarioNome { get; set; }
        public List<ComentarioDTO> Comentarios { get; set; }
    }
}