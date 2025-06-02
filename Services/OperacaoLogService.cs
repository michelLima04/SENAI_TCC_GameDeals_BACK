using GameDeals.API.Data;
using GameDeals.API.Models;
using System.Threading.Tasks;

public class OperacaoLogService
{
    private readonly AppDbContext _context;

    public OperacaoLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(int userId, string acao, string? entidadeAfetada = null, int? idEntidade = null, string? detalhes = null)
    {
        var log = new OperacaoLogModel
        {
            IdUsuario = userId,
            Acao = acao,
            EntidadeAfetada = entidadeAfetada,
            IdEntidade = idEntidade,
            Detalhes = detalhes
        };

        _context.OperacaoLog.Add(log);
        await _context.SaveChangesAsync();
    }
}
