using System.ComponentModel.DataAnnotations;

// Variáveis usadas para o Login do Usuário
public class UserLoginDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Senha { get; set; }
}

