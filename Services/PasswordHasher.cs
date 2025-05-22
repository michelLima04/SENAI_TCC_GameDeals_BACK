using System.Security.Cryptography;
using System.Text;

namespace GameDeals.API.Helpers
{
    public static class PasswordHasher
    {
        // Gera o hash da senha
        public static string Hash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Converte a senha em bytes
                var bytes = Encoding.UTF8.GetBytes(password);

                // Gera o hash
                var hash = sha256.ComputeHash(bytes);

                // Converte o hash para string Base64
                return Convert.ToBase64String(hash);
            }
        }

        // Compara uma senha com um hash salvo
        public static bool Verify(string password, string hashed)
        {
            return Hash(password) == hashed;
        }
    }
}
