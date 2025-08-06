using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using GameDeals.API.Data;

public class VerificadorDePromocoesService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VerificadorDePromocoesService> _logger;
    private readonly TimeSpan intervalo = TimeSpan.FromMinutes(5);

    public VerificadorDePromocoesService(IServiceProvider serviceProvider, ILogger<VerificadorDePromocoesService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de verificação de promoções iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scraper = scope.ServiceProvider.GetRequiredService<ScraperService>();

                var promocoesAtivas = await _context.Promocoes
                    .Where(p => p.StatusPublicacao)
                    .ToListAsync(stoppingToken);

                _logger.LogDebug("Verificando {Count} promoções ativas", promocoesAtivas.Count);

                var tarefas = promocoesAtivas
                    .Select(promocao => scraper.ExtrairDadosDaUrl(promocao.Url, apenasPreco: true))
                    .ToList();

                var resultados = await Task.WhenAll(tarefas);

                int desativadas = 0, atualizadas = 0;

                for (int i = 0; i < promocoesAtivas.Count; i++)
                {
                    var promocao = promocoesAtivas[i];
                    var (_, _, novoPreco, _, falhas) = resultados[i];

                    if (falhas.Any())
                    {
                        _logger.LogWarning("Falha ao verificar promoção {Id}: {Falhas}", promocao.Id, string.Join(", ", falhas));
                        continue;
                    }

                    if (novoPreco <= 0) 
                    {
                        promocao.StatusPublicacao = false;
                        promocao.MotivoInativacao = "Produto indisponível ou fora de estoque";
                        desativadas++;
                    }
                    else if (Math.Abs(novoPreco - promocao.Preco) > 0.01m)
                    {
                        if (novoPreco > promocao.Preco)
                        {
                            promocao.StatusPublicacao = false;
                            promocao.MotivoInativacao = $"Preço aumentou de {promocao.Preco} para {novoPreco}";
                            desativadas++;
                        }
                        else
                        {
                            promocao.Preco = novoPreco;
                            atualizadas++;
                        }
                    }
                }

                await _context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Verificação concluída: {Atualizadas} atualizadas, {Desativadas} desativadas",
                    atualizadas, desativadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante verificação de promoções");
            }

            await Task.Delay(intervalo, stoppingToken);
        }
    }
}