namespace GameDeals.API.DTOs
{
    public class UserResponseDTO
    {
        public int Id { get; set; }
        public string NomeSobrenome { get; set; }
        public string UsuarioNome { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }

    }
}
