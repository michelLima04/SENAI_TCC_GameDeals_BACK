using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameDeals.API.Data;
using GameDeals.API.Models;
using GameDeals.API.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace GameDeals.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComentarioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OperacaoLogService _logService;

        public ComentarioController(AppDbContext context, IHttpContextAccessor httpContextAccessor, OperacaoLogService logService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logService = logService;
        }

        [HttpPost("Cadastrar")]
        public async Task<IActionResult> CadastrarComentario([FromBody] ComentarioCreateDTO dto)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { mensagem = "Usuário não autenticado." });

            var emailUsuario = User.Identity.Name;

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == emailUsuario);

            if (usuario == null || usuario.IsAdmin)
                return Unauthorized(new { mensagem = "Apenas usuários padrão podem comentar." });

            var promocao = await _context.Promocoes
                .FirstOrDefaultAsync(p => p.Id == dto.IdPromocao && p.StatusPublicacao == true);

            if (promocao == null)
                return NotFound(new { mensagem = "Promoção não encontrada ou ainda não foi publicada." });

            bool isDono = promocao.UsuarioId == usuario.Id;

            var comentario = new Comentario
            {
                ComentarioTexto = dto.ComentarioTexto,
                DataComentario = DateTime.Now,
                IdUsuario = usuario.Id,
                IdPromocao = dto.IdPromocao,
                IsDono = isDono
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            await _logService.RegistrarAsync(
                usuario.Id,
                "Criou Comentário",
                "Comentario",
                comentario.Id,
                dto.ComentarioTexto
            );

            return Ok(new
            {
                mensagem = "Comentário cadastrado com sucesso.",
                comentario.Id,
                comentario.ComentarioTexto,
                comentario.DataComentario,
                comentario.IsDono,
                Usuario = usuario.UsuarioNome,
                comentario.IdPromocao
            });
        }

        [HttpPut("Alterar")]
        public async Task<IActionResult> AlterarComentario([FromBody] ComentarioUpdateDTO dto)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { mensagem = "Usuário não autenticado." });

            var emailUsuario = User.Identity.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailUsuario);
            if (usuario == null)
                return Unauthorized(new { mensagem = "Usuário não encontrado." });

            var comentario = await _context.Comentarios.FindAsync(dto.Id);
            if (comentario == null)
                return NotFound(new { mensagem = "Comentário não encontrado." });

            if (comentario.IdUsuario != usuario.Id)
                return Forbid("Você não tem permissão para alterar este comentário.");

            if (string.IsNullOrWhiteSpace(dto.ComentarioTexto))
                return BadRequest(new { mensagem = "O comentário não pode ser vazio." });

            comentario.ComentarioTexto = dto.ComentarioTexto;
            comentario.DataComentario = DateTime.Now;

            _context.Comentarios.Update(comentario);
            await _context.SaveChangesAsync();

            await _logService.RegistrarAsync(
                usuario.Id,
                "Alterou Comentário",
                "Comentario",
                comentario.Id,
                dto.ComentarioTexto
            );

            return Ok(new
            {
                mensagem = "Comentário alterado com sucesso.",
                comentario.Id,
                comentario.ComentarioTexto,
                comentario.DataComentario
            });
        }

        [HttpDelete("Deletar/{id}")]
        public async Task<IActionResult> DeletarComentario(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { mensagem = "Usuário não autenticado." });

            var emailUsuario = User.Identity.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailUsuario);
            if (usuario == null)
                return Unauthorized(new { mensagem = "Usuário não encontrado." });

            var comentario = await _context.Comentarios.FindAsync(id);
            if (comentario == null)
                return NotFound(new { mensagem = "Comentário não encontrado." });

            bool isAutor = comentario.IdUsuario == usuario.Id;
            bool isAdmin = usuario.IsAdmin;

            if (!isAutor && !isAdmin)
                return Forbid("Você não tem permissão para excluir este comentário.");

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            await _logService.RegistrarAsync(
                usuario.Id,
                "Deletou Comentário",
                "Comentario",
                comentario.Id,
                comentario.ComentarioTexto
            );

            return Ok(new { mensagem = "Comentário deletado com sucesso." });
        }
    }
}
