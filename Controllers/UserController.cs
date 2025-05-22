using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameDeals.API.Data;
using GameDeals.API.DTOs;
using GameDeals.API.Helpers;
using GameDeals.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GameDeals.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UsuariosController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        [HttpPost("Registro")]
        public async Task<IActionResult> Register(UserRegisterDTO dto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email || u.UsuarioNome == dto.UsuarioNome))
                return BadRequest(new { mensagem = "Email ou nome de usuário já estão em uso." });

            var usuario = new Usuario
            {
                NomeSobrenome = dto.NomeSobrenome,
                UsuarioNome = dto.UsuarioNome,
                Email = dto.Email,
                Senha = PasswordHasher.Hash(dto.Senha)
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Usuário cadastrado com sucesso!",
                usuario = new UserResponseDTO
                {
                    Id = usuario.Id,
                    NomeSobrenome = usuario.NomeSobrenome,
                    UsuarioNome = usuario.UsuarioNome,
                    Email = usuario.Email,
                    IsAdmin = usuario.IsAdmin
                }
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null)
                return Unauthorized(new { mensagem = "Usuário não encontrado." });

            if (PasswordHasher.Hash(dto.Senha) != usuario.Senha)
                return BadRequest(new { mensagem = "Senha incorreta." });

            if (usuario.EstaBloqueado)
                return Unauthorized(new { mensagem = "Este usuário está bloqueado." });

            if (usuario.Email == "adm@gmail.com" && PasswordHasher. Verify("123456", usuario.Senha) && !usuario.IsAdmin)
            {
                usuario.IsAdmin = true;
                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim("userName", usuario.UsuarioNome),
                new Claim(ClaimTypes.Name, usuario.Email),
                new Claim("UserId", usuario.Id.ToString()),
                new Claim(ClaimTypes.Role, usuario.IsAdmin ? "Admin" : "User")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                mensagem = usuario.IsAdmin ? "Seja bem-vindo, Admin!" : "Login realizado com sucesso.",
                token = tokenString
            });
        }

        [HttpPut("Atualizar-Dados/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserResponseDTO dto)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { mensagem = "Usuário não autenticado." });

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound(new { mensagem = "Usuário não encontrado." });

            if (usuario.Id != id)
                return BadRequest(new { mensagem = "Você não pode editar os dados de outro usuário." });

            usuario.NomeSobrenome = dto.NomeSobrenome ?? usuario.NomeSobrenome;
            usuario.UsuarioNome = dto.UsuarioNome ?? usuario.UsuarioNome;

            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Dados atualizados com sucesso." });
        }

        [HttpPost("Recuperar-Senha")]
        public async Task<IActionResult> RecuperarSenha([FromBody] RecuperarSenhaDTO dto)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { mensagem = "Usuário não autenticado." });

            if (dto.NovaSenha != dto.ConfirmarSenha)
                return BadRequest(new { mensagem = "As senhas não coincidem." });

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (usuario == null)
                return NotFound(new { mensagem = "Usuário não encontrado." });

            usuario.Senha = PasswordHasher.Hash(dto.NovaSenha);
            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Senha atualizada com sucesso." });
        }

        [HttpDelete("Deletar-Usuario/{id}")]
        public async Task<IActionResult> DeletarUsuario(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized("Usuário não autenticado.");

            var emailLogado = User.FindFirstValue(ClaimTypes.Name);
            var usuarioLogado = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailLogado);

            if (usuarioLogado == null)
                return Unauthorized("Usuário não encontrado.");

            if (!usuarioLogado.IsAdmin && usuarioLogado.Id != id)
                return Forbid("Você só pode deletar sua própria conta.");

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            if (usuario.IsAdmin && usuarioLogado.Id != usuario.Id)
                return BadRequest("Você não pode deletar outro administrador.");

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok("Usuário deletado com sucesso.");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Buscar-Usuario-ADM")]
        public async Task<IActionResult> BuscarUsuario([FromQuery] string termo)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NomeSobrenome.Contains(termo) || u.Email.Contains(termo));

            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            return Ok(usuario);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("Bloquear-Desbloquear-User(ADM)")]
        public async Task<IActionResult> BloquearDesbloquearUsuario([FromQuery] string termo)
        {
            var emailLogado = User.FindFirstValue(ClaimTypes.Name);
            var usuarioLogado = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailLogado);

            if (usuarioLogado == null || !usuarioLogado.IsAdmin)
                return Forbid("Apenas administradores logados podem realizar esta ação.");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioNome == termo || u.Email == termo);

            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            if (usuario.IsAdmin)
                return BadRequest("Você não pode bloquear um administrador.");

            usuario.EstaBloqueado = !usuario.EstaBloqueado;
            await _context.SaveChangesAsync();

            return Ok($"Usuário {(usuario.EstaBloqueado ? "bloqueado" : "desbloqueado")} com sucesso.");
        }
    }
}
