using Microsoft.AspNetCore.Mvc;
using GameDeals.API.Data;
using GameDeals.API.Models;
using GameDeals.API.DTOs;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Globalization;
using GameDeals.DTOs.PromocaoDTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using GameDeals.Models;


namespace AppPromocoesGamer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromocaoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PromocaoController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private int ObterUsuarioId()
        {
            var emailLogado = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "email")?.Value;
            var usuarioLogado = _context.Usuarios.FirstOrDefault(u => u.Email == emailLogado);
            return usuarioLogado?.Id ?? 0;
        }

        private bool UsuarioEhAdmin()
        {
            var emailLogado = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "email")?.Value;
            var usuarioLogado = _context.Usuarios.FirstOrDefault(u => u.Email == emailLogado);
            return usuarioLogado?.IsAdmin ?? false;
        }

        private async Task<(string titulo, string imagemUrl, decimal preco, string siteVendedor, List<string> falhas)>
        ExtrairDadosDaUrl(string url)
        {
            var falhas = new List<string>();
            string titulo = null;
            string imagemUrl = null;
            decimal preco = 0;
            string siteVendedor = null;

            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");

                var html = await httpClient.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var host = new Uri(url).Host.Replace("www.", "").ToLower();
                var partes = host.Split('.');

                var sufixosCompostos = new[] { "com.br", "org.br", "net.br", "gov.br" };
                var dominioFinal = string.Join('.', partes.Skip(partes.Length - 2));
                if (sufixosCompostos.Contains(dominioFinal) && partes.Length >= 3)
                {
                    siteVendedor = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(partes[partes.Length - 3]);
                }
                else if (partes.Length >= 2)
                {
                    siteVendedor = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(partes[partes.Length - 2]);
                }
                else
                {
                    siteVendedor = host;
                }

                var tituloNode = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='productTitle']");
                if (tituloNode != null)
                {
                    var tituloCompleto = WebUtility.HtmlDecode(tituloNode.InnerText.Trim());
                    titulo = tituloCompleto;
                }
                else
                {
                    falhas.Add("Não foi possível extrair o título.");
                }


                string precoTexto = null;
                var precoNode = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='a-offscreen']") ?? htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class,'a-price-whole')]");
                if (precoNode != null)
                {
                    precoTexto = precoNode.InnerText;
                }
                else
                {
                    var match = Regex.Match(htmlDoc.DocumentNode.InnerText, @"R?\$\s?\d{1,3}(\.\d{3})*,\d{2}");
                    if (match.Success)
                    {
                        precoTexto = match.Value;
                    }
                }

                if (!string.IsNullOrWhiteSpace(precoTexto))
                {
                    precoTexto = precoTexto.Replace("R$", "").Replace("\u00A0", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(precoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precoConvertido))
                    {
                        preco = Math.Round(precoConvertido, 2);
                    }
                    else if (int.TryParse(precoTexto, out int precoCentavos))
                    {
                        preco = precoCentavos / 100m;
                    }
                    else
                    {
                        falhas.Add("Preço extraído, mas não foi possível converter.");
                    }
                }
                else
                {
                    falhas.Add("Não foi possível extrair o preço.");
                }

                var imagemNode = htmlDoc.DocumentNode.SelectSingleNode("//img[@id='landingImage']") ?? htmlDoc.DocumentNode.SelectSingleNode("//img[@data-old-hires]");
                if (imagemNode != null)
                {
                    imagemUrl = imagemNode.GetAttributeValue("src", null);
                }
                else
                {
                    falhas.Add("Não foi possível extrair a imagem.");
                }
            }
            catch (Exception ex)
            {
                falhas.Add($"Erro durante scraping: {ex.Message}");
            }

            return (titulo, imagemUrl, preco, siteVendedor, falhas);
        }

        [HttpPost("Cadastrar")]
        [Authorize]
        public async Task<IActionResult> PostPromocao([FromBody] PromocaoCreateDTO dto)
        {
            var userEmail = User.Identity.Name;

            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized("Token inválido ou expirado.");
            }

            var valid_promo = await _context.Promocoes.FirstOrDefaultAsync(u => u.Url == dto.UrlPromocao);

            if (valid_promo != null && (valid_promo.StatusPublicacao || valid_promo.Url != ""))
            {
                return BadRequest(new { mensagem = "Já contem uma promoção em andamento." });
            }

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user.IsAdmin)
                return StatusCode(403, new { mensagem = "Administradores não podem cadastrar promoções." });

            if (string.IsNullOrWhiteSpace(dto.UrlPromocao))
                return BadRequest(new { mensagem = "A URL da promoção não pode estar vazia." });

            if (!Uri.TryCreate(dto.UrlPromocao, UriKind.Absolute, out Uri uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new { mensagem = "A URL fornecida não é válida." });
            }

            var (tituloTemp, _, _, _, falhasTemp) = await ExtrairDadosDaUrl(dto.UrlPromocao);

            string RemoverAcentos(string texto)
            {
                return new string(texto
                    .Normalize(NormalizationForm.FormD)
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .ToArray())
                    .ToLower();
            }

            var categoriasGamer = new[]
            {
                "amd", "cooler", "cpu", "fontes", "gabinete", "gpu", "gtx", "intel", "placa mãe", "ram", "rtx", "ssd", "water cooler", "ryzen",
                "computador", "monitor", "cadeira", "controle", "fones", "headset", "microfone", "mouse", "mousepad", "rgb", "teclado",
                "hololens", "htc", "vive", "vr", "óculos", "câmera", "deck", "placa captura", "ring light", "stream", "transmissão",
                "hub", "modem", "roteador", "switch", "wifi", "hd externo", "nintendo", "ps4", "ps5", "xbox", "digital", "fisica", "game",
                "games", "gamer", "jogo", "jogos", "steam", "fan", "overclock", "pasta térmica", "ventoinha", "cabo", "organizador", "suporte",
                "frigobar", "gaming", "reddragon"
            };

            var tituloNormalizado = RemoverAcentos(tituloTemp ?? "");

            int matchCount = categoriasGamer
                .Select(label => RemoverAcentos(label))
                .Count(label => tituloNormalizado.Contains(label));

            if (matchCount < 2)
            {
                return BadRequest(new { mensagem = "Este produto não é compatível ao nicho Gamer." });
            }

            // Extração completa dos dados
            var (titulo, imagemUrl, preco, siteVendedor, falhas) = await ExtrairDadosDaUrl(dto.UrlPromocao);

            if (string.IsNullOrWhiteSpace(titulo))
                falhas.Add("Título não encontrado.");

            if (preco <= 0)
                falhas.Add("Preço não extraído corretamente.");

            if (string.IsNullOrWhiteSpace(siteVendedor))
                falhas.Add("Vendedor não encontrado.");

            if (string.IsNullOrWhiteSpace(imagemUrl))
                falhas.Add("Imagem não encontrada.");

            if (falhas.Any())
            {
                return BadRequest(new
                {
                    mensagem = "Promoção não cadastrada. Dados faltando ou inválidos.",
                    falhas
                });
            }

            var promocao = new Promocao
            {
                UsuarioId = user.Id,
                Url = dto.UrlPromocao,
                Titulo = titulo,
                Preco = preco,
                Site = siteVendedor,
                TempoPostado = DateTime.Now.TimeOfDay,
                Cupom = dto.Cupom,
                ImagemUrl = imagemUrl,
                StatusPublicacao = true
            };

            if (dto.isAdd)
            {
                _context.Promocoes.Add(promocao);
                await _context.SaveChangesAsync();
            }



            return Ok(new
            {
                promocao,
                mensagem = "Promoção cadastrada com sucesso!"
            });
        }

        [HttpGet("Feed")]
        public async Task<IActionResult> ListarFeed()
        {
            var promocoes = await _context.Promocoes
                .Include(p => p.Usuario)
                .Where(p => p.StatusPublicacao)
                .ToListAsync();

            await _context.SaveChangesAsync();

            var promocoesAtivas = _context.Promocoes
             .Where(p => p.StatusPublicacao)
             .Select(p => new
             {
                 p.Id,
                 p.Titulo,
                 p.Preco,
                 p.Cupom,
                 p.ImagemUrl,
                 p.Site,
                 p.TempoPostado,
                 p.CreatedAt,
                 UsuarioNome = _context.Usuarios
                     .Where(u => u.Id == p.UsuarioId)
                     .Select(u => u.UsuarioNome)
                     .FirstOrDefault(),
                 QuantidadeComentarios = _context.Comentarios.Count(c => c.IdPromocao == p.Id),
                 QuantidadeCurtidas = _context.Curtidas.Count(c => c.id_promocao == p.Id)
             })
             .AsEnumerable()
             .Select(p => new
             {
                 p.Id,
                 p.Titulo,
                 p.Preco,
                 p.Cupom,
                 p.ImagemUrl,
                 p.Site,
                 p.TempoPostado,
                 p.UsuarioNome,
                 p.QuantidadeComentarios,
                 p.QuantidadeCurtidas,
                 TempoDecorrido = CalcularTempoDecorrido(p.CreatedAt)
             })
             .ToList();


            return Ok(promocoesAtivas);
        }

        [HttpPost("Feed/{id}/like")]
        [Authorize]
        public async Task<IActionResult> LikeFeed(int id)
        {
            try
            {
                var userEmail = User.Identity.Name;

                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized("Token inválido ou expirado.");
                }

                var user = await _context.Usuarios.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (user == null)
                {
                    return NotFound("Usuário não encontrado.");
                }

                int userId = user.Id;

                bool jaCurtiu = await _context.Curtidas
                    .AnyAsync(l => l.id_usuario == userId && l.id_promocao == id);

                if (jaCurtiu)
                {
                    return BadRequest("Você já curtiu esse post.");
                }

                var like = new Curtidas
                {
                    id_usuario = userId,
                    id_promocao = id,
                    created_at = DateTime.UtcNow
                };

                _context.Curtidas.Add(like);
                await _context.SaveChangesAsync();

                int quantidadeCurtidas = await _context.Curtidas
                    .CountAsync(c => c.id_promocao == id);

                return Ok(new
                {
                    id = id,
                    quantidadeCurtidas = quantidadeCurtidas,
                    jaCurtido = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }

        [HttpGet("Feed/test")]
        public async Task<IActionResult> ListarTest()
        {
            var comments = await _context.Comentarios.ToListAsync();



            return Ok(comments);
        }

        private string RemoverAcentos(string texto)
        {
            var normalized = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().ToLower();
        }

        [HttpGet("Buscar")]
        public async Task<IActionResult> BuscarPromocoes([FromQuery] string nomeProduto)
        {
            if (string.IsNullOrWhiteSpace(nomeProduto))
                return BadRequest(new { mensagem = "O nome do produto não pode estar vazio." });

            var nomeProdutoNormalizado = RemoverAcentos(nomeProduto);

            var promocoesEncontradas = await _context.Promocoes
               .Where(p => p.StatusPublicacao)
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    p.Preco,
                    p.Cupom,
                    p.ImagemUrl,
                    p.Site,
                    p.TempoPostado,
                    QuantidadeComentarios = _context.Comentarios.Count(c => c.IdPromocao == p.Id),
                    QuantidadeCurtidas = _context.Curtidas.Count(c => c.id_promocao == p.Id)
                })
                .ToListAsync();

            if (!promocoesEncontradas.Any())
                return NotFound(new { mensagem = "Nenhuma promoção encontrada para o nome informado." });

            return Ok(promocoesEncontradas);
        }

        [HttpDelete("Excluir/{id}")]
        public async Task<IActionResult> ExcluirPromocao([FromRoute] int id)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized("Usuário não autenticado.");

            bool usuarioEhAdmin = UsuarioEhAdmin();

            var promocao = await _context.Promocoes.FindAsync(id);
            if (promocao == null)
                return NotFound("Promoção não encontrada.");

            var usuarioLogado = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);

            if (usuarioLogado == null)
                return Unauthorized("Usuário não encontrado.");

            if (!usuarioEhAdmin && promocao.UsuarioId != usuarioLogado.Id)
            {
                return Forbid("Você não tem permissão para excluir esta promoção.");
            }

            _context.Promocoes.Remove(promocao);
            await _context.SaveChangesAsync();

            return Ok("Promoção excluída com sucesso.");
        }


        private static string CalcularTempoDecorrido(DateTime createdAt)
        {
            var tempo = DateTime.Now - createdAt;

            if (tempo.TotalMinutes < 1)
                return "agora mesmo";

            int dias = tempo.Days;
            int horas = tempo.Hours;
            int minutos = tempo.Minutes;

            string resultado = "há ";

            if (dias > 0)
                resultado += $"{dias} dia{(dias > 1 ? "s" : "")} ";

            if (horas > 0)
                resultado += $"{horas} hora{(horas > 1 ? "s" : "")} ";

            if (minutos > 0)
                resultado += $"{minutos} minuto{(minutos > 1 ? "s" : "")} ";

            return resultado.Trim() + " atrás";
        }

    }
    }
