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

// Serviço para Verificar o preço de todos os produtos do sistema, afim de atualizar o preço ou inativar uma promoção
public class VerificadorDePromocoesService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VerificadorDePromocoesService> _logger;
    private readonly TimeSpan intervalo = TimeSpan.FromMinutes(5); // A cada 5 minutos a verificação, a partir que a API começar.

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
                    .Where(p => p.StatusPublicacao == true)
                    .ToListAsync(stoppingToken);

                var tarefas = new List<Task<(string, string, decimal, string, List<string>)>>();

                foreach (var promocao in promocoesAtivas)
                {
                    tarefas.Add(scraper.ExtrairDadosDaUrl(promocao.Url, apenasPreco: true));
                    // Método do Scrapper para verificar apenas o preço dos produtos Ativos
                }

                var resultados = await Task.WhenAll(tarefas);

                for (int i = 0; i < promocoesAtivas.Count; i++)
                {
                    var promocao = promocoesAtivas[i];
                    var (_, _, novoPreco, _, falhas) = resultados[i];

                    if (!falhas.Any())
                    {
                        if (novoPreco > promocao.Preco)
                        {
                            // Se o preço novo for MAIOR que o preço antigo, a promoção será inativada.
                            promocao.StatusPublicacao = false;
                            promocao.MotivoInativacao = "Alteração de preço - Aumentou!";
                        }
                        else if (novoPreco < promocao.Preco)
                        {
                            promocao.Preco = novoPreco;
                            // Se o preço novo for MENOR ao preço antigo, o preço será atualizado.
                        }
                    }
                }

                await _context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Verificação de promoções concluída: {count} promoções verificadas", promocoesAtivas.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante verificação de promoções");
            }

            await Task.Delay(intervalo, stoppingToken);
        }
    }
}
