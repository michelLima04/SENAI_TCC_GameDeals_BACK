using System.ComponentModel.DataAnnotations;

// Variáveis usadas para Registrar um Usuário
public class UserRegisterDTO
{
    [Required]
    public string NomeSobrenome { get; set; }

    [Required]
    public string UsuarioNome { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Senha { get; set; }
}

