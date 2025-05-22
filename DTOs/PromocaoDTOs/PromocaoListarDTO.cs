namespace GameDeals.DTOs.PromocaoDTOs
{
    public class PromocaoListDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public decimal Preco { get; set; }
        public string Cupom { get; set; }
        public string Site { get; set; }
        public string ImagemUrl { get; set; }
        public string TempoPostado { get; set; } 
    }
}
