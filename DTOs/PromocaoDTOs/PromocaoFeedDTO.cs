
// Variáveis usadas para Listar as Promoções
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
    public string Cupom { get; set; }
    public List<ComentarioCreateDTO> Comentarios { get; set; }

    public int Likes { get; set; }
    public bool IsLiked { get; set; }


}
