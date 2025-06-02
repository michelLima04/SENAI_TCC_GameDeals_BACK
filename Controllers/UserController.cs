using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameDeals.API.Data;
using GameDeals.API.DTOs;
using GameDeals.API.Helpers;
using GameDeals.API.Models;
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
        private readonly OperacaoLogService _logService;

        public UsuariosController(AppDbContext context, IConfiguration configuration, OperacaoLogService logService)
        {
            _context = context;
            _configuration = configuration;
            _logService = logService;
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
                Senha = PasswordHasher.Hash(dto.Senha),
                CriadoEm = DateTime.Today
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            await _logService.RegistrarAsync(
                usuario.Id,
                "Registro",
                "Usuario",
                usuario.Id,
                $"Usuário '{usuario.UsuarioNome}' foi registrado."
            );

            return Ok(new
            {
                mensagem = "Usuário cadastrado com sucesso!",
                usuario = new UserResponseDTO
                {
                    Id = usuario.Id,
                    NomeSobrenome = usuario.NomeSobrenome,
                    UsuarioNome = usuario.UsuarioNome,
                    Email = usuario.Email,
                    IsAdmin = usuario.IsAdmin,
                    CriadoEm = usuario.CriadoEm,
                    Contribuicoes = usuario.Contribuicoes
                }
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null)
                return Unauthorized(new { mensagem = "Usuário não encontrado." });

            if (!PasswordHasher.Verify(dto.Senha, usuario.Senha))
                return BadRequest(new { mensagem = "Senha incorreta." });

            if (usuario.EstaBloqueado)
                return Unauthorized(new { mensagem = "Este usuário está bloqueado." });

            if (usuario.Email == "adm@gmail.com" && !usuario.IsAdmin)
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

            await _logService.RegistrarAsync(
                usuario.Id,
                "Login",
                "Usuario",
                usuario.Id,
                $"Usuário '{usuario.UsuarioNome}' fez login."
            );

            return Ok(new
            {
                mensagem = usuario.IsAdmin ? "Seja bem-vindo, Admin!" : "Login realizado com sucesso.",
                token = tokenString
            });
        }

        [Authorize]
        [HttpPut("Profile/editar")]
        public async Task<IActionResult> EditarPerfil([FromBody] EditarPerfilDTO dto)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Usuário não autenticado.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            usuario.NomeSobrenome = dto.NomeSobrenome;
            usuario.UsuarioNome = dto.UsuarioNome;
            usuario.Email = dto.Email;

            await _logService.RegistrarAsync(
                usuario.Id,
                "EditarPerfil",
                "Usuario",
                usuario.Id,
                $"Usuário '{usuario.UsuarioNome}' atualizou seu perfil."
            );

            await _context.SaveChangesAsync();
            return Ok(new { message = "Perfil atualizado com sucesso" });
        }

        [Authorize]
        [HttpPut("Profile/trocar-senha")]
        public async Task<IActionResult> TrocarSenha([FromBody] TrocarSenhaDTO dto)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Usuário não autenticado.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            if (!PasswordHasher.Verify(dto.SenhaAtual, usuario.Senha))
                return BadRequest("Senha atual incorreta.");

            usuario.Senha = PasswordHasher.Hash(dto.NovaSenha);
            await _context.SaveChangesAsync();

            await _logService.RegistrarAsync(
                usuario.Id,
                "TrocarSenha",
                "Usuario",
                usuario.Id,
                $"Usuário '{usuario.UsuarioNome}' alterou sua senha."
            );

            return Ok(new { message = "Senha atualizada com sucesso" });
        }

        [Authorize]
        [HttpGet("Profile/me")]
        public async Task<IActionResult> ObterPerfil()
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Usuário não autenticado.");

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            var totalPromocoes = await _context.Promocoes
                .CountAsync(p => p.UsuarioId == usuario.Id);

            var logs = await _context.OperacaoLog
                .Where(log => log.IdUsuario == usuario.Id)
                .OrderByDescending(log => log.CreatedAt)
                .Take(10)
                .Select(log => new
                {
                    acao = log.Acao,
                    entidadeAfetada = log.EntidadeAfetada,
                    idEntidade = log.IdEntidade,
                    detalhes = log.Detalhes,
                    createdAt = log.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                })
                .ToListAsync();

            return Ok(new
            {
                fullName = usuario.NomeSobrenome,
                username = "@" + usuario.UsuarioNome,
                email = usuario.Email,
                joinDate = usuario.CriadoEm.ToString("dd/MM/yyyy"),
                contributions = totalPromocoes,
                logs = logs
            });
        }

        [Authorize]
        [HttpPut("Profile/atualizar-contribuicoes")]
        public async Task<IActionResult> AtualizarContribuicoes()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Usuário não autenticado.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            int totalPromocoes = await _context.Promocoes.CountAsync(p => p.UsuarioId == usuario.Id);
            usuario.Contribuicoes = totalPromocoes;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contribuições atualizadas", total = totalPromocoes });
        }

        public class EditarPerfilDTO
        {
            public string NomeSobrenome { get; set; } = string.Empty;
            public string UsuarioNome { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class TrocarSenhaDTO
        {
            public string SenhaAtual { get; set; } = string.Empty;
            public string NovaSenha { get; set; } = string.Empty;
        }
    }
}
