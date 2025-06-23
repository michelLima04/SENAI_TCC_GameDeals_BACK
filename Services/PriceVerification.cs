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
                    .Where(p => p.StatusPublicacao == 1)
                    .ToListAsync(stoppingToken);

                var tarefas = new List<Task<(string, string, decimal, string, List<string>)>>();

                foreach (var promocao in promocoesAtivas)
                {
                    tarefas.Add(scraper.ExtrairDadosDaUrl(promocao.Url, apenasPreco: true));
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
                            promocao.StatusPublicacao = 0;
                        }
                        else if (novoPreco < promocao.Preco)
                        {
                            promocao.Preco = novoPreco;
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
