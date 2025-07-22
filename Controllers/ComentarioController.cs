using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameDeals.API.Data;
using GameDeals.API.Models;

namespace GameDeals.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComentarioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ComentarioController(AppDbContext context, IHttpContextAccessor httpContextAccessor, OperacaoLogService logService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Rota para Criar um Comentário
        [HttpPost("Cadastrar")]
        public async Task<IActionResult> CadastrarComentario([FromBody] ComentarioCreateDTO dto)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { mensagem = "Usuário não autenticado." });

            var emailUsuario = User.Identity.Name;

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == emailUsuario);

            var promocao = await _context.Promocoes
                .FirstOrDefaultAsync(p => p.Id == dto.IdPromocao && p.StatusPublicacao == true);

            if (promocao == null)
                return NotFound(new { mensagem = "Promoção não encontrada ou ainda não foi publicada." });

            var comentario = new Comentario
            {
                ComentarioTexto = dto.ComentarioTexto,
                DataComentario = DateTime.Now,
                IdUsuario = usuario.Id,
                IdPromocao = dto.IdPromocao,
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Comentário cadastrado com sucesso.",
                comentario.Id,
                comentario.ComentarioTexto,
                comentario.DataComentario,
                Usuario = usuario.UsuarioNome,
                comentario.IdPromocao
            });
        }

        // Rota para Alterar um Comentário
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

            return Ok(new
            {
                mensagem = "Comentário alterado com sucesso.",
                comentario.Id,
                comentario.ComentarioTexto,
                comentario.DataComentario
            });
        }

        // Rota para Excluir um comentário
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

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Comentário deletado com sucesso." });
        }
    }
}
