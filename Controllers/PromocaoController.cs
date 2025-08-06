using Microsoft.AspNetCore.Mvc;
using GameDeals.API.Data;
using GameDeals.API.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using GameDeals.Models;
using GameDeals.DTOs.PromocaoDTOs;
using System.Security.Claims;

namespace AppPromocoesGamer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromocaoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OperacaoLogService _logService;
        private IEnumerable<string> testc;

        public PromocaoController(AppDbContext context, IHttpContextAccessor httpContextAccessor, OperacaoLogService logService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logService = logService;
        }

        private async Task<(string titulo, string imagemUrl, decimal preco, string siteVendedor, List<string> falhas)>

        // Método para extrair os dados de uma Url (Scrapper) para o cadastro de uma promoção
        ExtrairDadosDaUrl(string url, bool apenasPreco = false)
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

                if (!apenasPreco)
                {
                    // Extrair nome do Domínio, por exemplo "Amazon"
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

                    // Extrair título do produto
                    var tituloNode = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='productTitle']");
                    if (tituloNode != null)
                    {
                        titulo = WebUtility.HtmlDecode(tituloNode.InnerText.Trim());
                    }
                    else
                    {
                        falhas.Add("Não foi possível extrair o título.");
                    }

                    // Extrair imagem do produto
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

                // Extrair preço atual do produto
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
            }
            catch (Exception ex)
            {
                falhas.Add($"Erro durante scraping: {ex.Message}");
            }

            return (titulo, imagemUrl, preco, siteVendedor, falhas);
        }

        // Lista de nomes de produtos do nicho Gamer para a validação posterior
        public static class CategoriasGamer
        {
            public static readonly string[] Lista = new[]
            {
                // Hardware (PC e componentes)
                "amd", "cooler", "cpu", "fontes", "gabinete", "gpu", "gtx", "intel", "placa", "mãe", "ram", "rtx", "ssd", "water", "ryzen",
                "hdd", "nvme", "m.2", "air", "thermal", "pad", "pasta", "térmica", "ventoinha", "fan", "overclock", "cooling",
                "chipset", "threadripper", "core", "i9", "i7", "i5", "i3", "zen", "epyc", "raid", "sata", "pcie", "psu",
                "fonte", "modular", "80", "plus", "crossfire", "sli", "vrm", "heatsink", "radiador", "aio", "custom", "loop",
                "ddr1", "ddr2", "ddr3", "ddr4", "ddr5", "notebook", "laptop",

                // Periféricos
                "monitor", "cadeira", "controle", "fones", "headset", "microfone", "mouse", "mousepad", "rgb", "teclado",
                "mecânico", "membrana", "switch", "óptico", "laser", "dpi", "webcam", "ring", "light", "captura",
                "hdmi", "displayport", "usb", "suporte", "mesa", "led", "condensador", "dinâmico", "pop", "filter",
                "articulado", "gamepad", "joystick", "trackball", "touchpad", "volante", "pedal", "ergonômico",

                // Realidade Virtual e Aumentada
                "hololens", "htc", "vive", "vr", "óculos", "oculus", "quest", "rift", "valve", "index", "ar", "mixed",
                "reality", "motion", "tracking", "controlador", "sensor", "base", "station", "headset",

                // Consoles e Jogos 
                "nintendo", "ps4", "ps5", "4", "5", "xbox", "series", "one", "switch", "gamecube", "wii",
                "playstation", "dualshock", "dualsense", "joy-con", "game", "pass", "plus", "online",
                "digital", "física", "games", "gamer", "gaming", "jogo", "jogos", "steam", "epic", "battle", "net",
                "origin", "uplay", "retro", "emulador", "arcade", "console", "edicao", "padrao", 

                // Streaming e Conectividade
                "stream", "transmissão", "twitch", "youtube", "obs", "streamlabs", "elgato", "green", "screen",
                "chroma", "key", "câmera", "hub", "modem", "roteador", "wifi", "mesh", "extensor",
                "powerline", "ethernet", "rj45", "fibra", "óptica", "wireless", "dongle",

                // Armazenamento e Acessórios
                "hd", "externo", "pen", "drive", "cartão", "sd", "microsd", "nas", "servidor", "backup", "cloud", "storage",
                "case", "docking", "station", "cabo", "organizador", "management", "tie", "wrap", "leitor",

                // Outros (Estilo e Conforto Gamer)
                "frigobar", "reddragon", "razer", "logitech", "hyperx", "corsair", "steelseries", "asus", "rog",
                "msi", "gigabyte", "nzxt", "thermaltake", "alienware", "acer", "predator", "lenovo", "legion",
                "hp", "omen", "deskmat", "luz", "ambiente", "painel", "setup", "customização", "skins",
                "recarga", "bateria", "suporte", "headset", "purificador", "ar",

                // Tendências e Miscelânea
                "esports", "battle", "royale", "open", "world", "rpg", "fps", "moba", "indie", "cloud", "ray", "tracing",
                "dlss", "fsr", "4k", "8k", "120", "144", "240", "hz", "ultrawide", "curvo", "portátil",
                "smart", "glasses", "wearable", "tecnologia", "háptica", "tátil", "crossplay", "modding",
                "waterblock", "controller", "alexa"
            };

        }

        // Rota para Cadastrar uma promoção
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

            if (valid_promo != null && (valid_promo.StatusPublicacao != true || valid_promo.Url != ""))
            {
                return BadRequest(new { mensagem = "Já contém uma promoção em andamento." });
                // Retorno para evitar postagens de promoções com a mesma Url
            }

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (string.IsNullOrWhiteSpace(dto.UrlPromocao))
                return BadRequest(new { mensagem = "A URL da promoção não pode estar vazia." });

            if (!Uri.TryCreate(dto.UrlPromocao, UriKind.Absolute, out Uri uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new { mensagem = "A URL fornecida não é válida." });
                // Retorno para validar se a Url é válida
            }

            var (tituloTemp, _, _, _, falhasTemp) = await ExtrairDadosDaUrl(dto.UrlPromocao);

            // Metódo para pegar o título do produto para remover acentos e minimizar todos os caracteres, para afim de validar as palavras
            string RemoverAcentos(string texto)
            {
                return new string(texto
                    .Normalize(NormalizationForm.FormD)
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .ToArray())
                    .ToLower();
            }

            var tituloNormalizado = RemoverAcentos(tituloTemp ?? "");

            int matchCount = 0;

            testc = CategoriasGamer.Lista.Select(label => RemoverAcentos(label));

            matchCount = CategoriasGamer.Lista
                .Select(label => RemoverAcentos(label))
                .Count(label => tituloNormalizado.Split(" ").Contains(label));
            
            // Para a verificação para validar se o produto é Gamer, optamos por fazer um contador
            // que verifica cada palavra(string) presente no título do produto, para cada mesma palavra 
            // que estiver presente na Lista de produtos Gamer ele irá pontuar ++1, quando este contador
            // for == 2, consideramos que ele é um produto Gamer

            if (matchCount < 2)
            {
                return BadRequest(new { mensagem = "Este produto não é compatível ao nicho Gamer." });
            }

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

                user.Contribuicoes += 1;
                _context.Usuarios.Update(user);

                await _context.SaveChangesAsync();

                await _logService.RegistrarAsync(
                    user.Id,
                    "Cadastro",
                    "Promocao",
                    promocao.Id,
                    $"Promoção '{promocao.Titulo}' cadastrada pelo usuário {user.UsuarioNome}."
                );
            }

            return Ok(new
            {
                promocao,
                mensagem = "Promoção cadastrada com sucesso!"
            });
        }

        // Rota para listar todos os Produtos Ativos -> Feed na página Home
        [HttpGet("Feed")]
        public async Task<IActionResult> ListarFeed([FromQuery] string? titulo)
        {
            var query = _context.Promocoes
                .Include(p => p.Usuario)
                .Where(p => p.StatusPublicacao == true); // Listar apenas promoções ativas

            if (!string.IsNullOrEmpty(titulo))
            {
                query = query.Where(p => p.Titulo.Contains(titulo));
            }

            var promocoes = await query
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    p.Preco,
                    p.Cupom,
                    p.ImagemUrl,
                    p.Site,
                    p.TempoPostado,
                    p.StatusPublicacao,
                    p.CreatedAt,
                    p.Usuario.UsuarioNome,
                    QuantidadeComentarios = _context.Comentarios.Count(c => c.IdPromocao == p.Id),
                    QuantidadeCurtidas = _context.Curtidas.Count(c => c.id_promocao == p.Id),
                    TempoDecorrido = CalcularTempoDecorrido(p.CreatedAt)
                })
                .ToListAsync();

            return Ok(promocoes);
        }

        // Rota para entrar no Card de uma Promoção
        [HttpGet("Feed/{id}")]
        public async Task<IActionResult> FindPromo(int id)
        {
            var promocao = await _context.Promocoes
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promocao == null)
                return NotFound();

            int quantidadeCurtidas = await _context.Curtidas.CountAsync(c => c.id_promocao == id);

            // Verifica se o usuário está autenticado
            var userEmail = User.Identity?.Name;
            bool isLiked = false;

            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userEmail))
            {
                var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user != null)
                {
                    isLiked = await _context.Curtidas
                        .AnyAsync(c => c.id_promocao == id && c.id_usuario == user.Id);
                }
            }

            var result = new PromocaoFeedDTO
            {
                Id = promocao.Id,
                Url = promocao.Url,
                Titulo = promocao.Titulo,
                Site = promocao.Site,
                Preco = promocao.Preco,
                ImagemUrl = promocao.ImagemUrl,
                CreatedAt = promocao.CreatedAt,
                UsuarioNome = promocao.Usuario.UsuarioNome,
                Cupom = promocao.Cupom,
                Comentarios = promocao.Comentarios.Select(c => new ComentarioCreateDTO
                {
                    Id = c.Id,
                    ComentarioTexto = c.ComentarioTexto,
                    DataComentario = c.DataComentario,
                    UsuarioNome = c.Usuario.UsuarioNome
                }).ToList(),
                Likes = quantidadeCurtidas,
                IsLiked = isLiked
            };

            return Ok(result);
        }


        // Rota para Curtir uma Promoção
        [HttpPost("Feed/{id}/like")]
        [Authorize]
        public async Task<IActionResult> LikeFeed(int id)
        {
            try
            {
                var userEmail = User.Identity?.Name;

                if (!User.Identity.IsAuthenticated || userEmail == null)
                {
                    return Unauthorized("Token inválido ou expirado.");
                }

                var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return NotFound("Usuário não encontrado.");
                }

                int userId = user.Id;

                var curtidaExistente = await _context.Curtidas
                    .FirstOrDefaultAsync(c => c.id_usuario == userId && c.id_promocao == id);

                if (curtidaExistente != null)
                {
                    // Se curtiu: vamos remover (descurtir)
                    _context.Curtidas.Remove(curtidaExistente);
                    await _context.SaveChangesAsync();

                    int novaQuantidade = await _context.Curtidas.CountAsync(c => c.id_promocao == id);

                    return Ok(new
                    {
                        id = id,
                        quantidadeCurtidas = novaQuantidade,
                        jaCurtido = false
                    });
                }

                // Ainda não curtiu: vamos adicionar
                var novaCurtida = new Curtidas
                {
                    id_usuario = userId,
                    id_promocao = id,
                    created_at = DateTime.UtcNow
                };

                _context.Curtidas.Add(novaCurtida);
                await _context.SaveChangesAsync();

                int quantidadeCurtidas = await _context.Curtidas.CountAsync(c => c.id_promocao == id);

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

        // Rota para Buscar uma Promoção a partir de uma palavra/string -> Barra de Pesquisa
        [HttpGet("Buscar")]
        public async Task<IActionResult> BuscarPromocoes([FromQuery] string nomeProduto)
        {
            if (string.IsNullOrWhiteSpace(nomeProduto))
                return BadRequest(new { mensagem = "O nome do produto não pode estar vazio." });

            var nomeProdutoLower = nomeProduto.ToLower();

            var promocoesEncontradas = await _context.Promocoes
                .Where(p => p.StatusPublicacao == true && p.Titulo.ToLower().Contains(nomeProdutoLower))
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    p.Preco,
                    p.Cupom,
                    p.ImagemUrl,
                    p.Site,
                    p.TempoPostado,
                    p.StatusPublicacao,
                    p.Usuario.UsuarioNome,
                    p.CreatedAt,
                    QuantidadeComentarios = _context.Comentarios.Count(c => c.IdPromocao == p.Id),
                    QuantidadeCurtidas = _context.Curtidas.Count(c => c.id_promocao == p.Id)
                })
                .ToListAsync();

            if (!promocoesEncontradas.Any())
                return NotFound(new { mensagem = "Nenhuma promoção encontrada para o nome informado." });

            return Ok(promocoesEncontradas);
        }


        // Rota para Deletar uma Promoção, apenas o usuário criador da postagem pode prosseguir aqui
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> ExcluirPromocao(int id)
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

            int usuarioId = user.Id;

            var promocao = await _context.Promocoes.FindAsync(id);

            if (promocao == null)
            {
                return NotFound(new { mensagem = "Promoção não encontrada." });
            }

            // Verifica se o usuário é o dono da promoção
            if (promocao.UsuarioId != usuarioId)
            {
                return Forbid("Você só pode excluir suas próprias promoções.");
            }

            // Marca como inativa, sem remover do banco
            promocao.StatusPublicacao = false;
            promocao.MotivoInativacao = "Criador do post excluiu a publicação.";
            promocao.TempoPostado = DateTime.Now.TimeOfDay;


            // Salva as alterações no banco
            await _context.SaveChangesAsync();

            await _logService.RegistrarAsync(
                    user.Id,
                    "Exclusão",
                    "Promocao",
                    promocao.Id,
                    $"Promoção '{promocao.Titulo}' excluída pelo usuário {user.UsuarioNome}."
            );

            promocao.MotivoInativacao = "Criador do post excluiu a publicação.";
            return Ok(new { mensagem = "Promoção excluída com sucesso." });
        }

        // Método para carcular há quanto tempo foi postado a Promoção do Produto
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
                resultado += $"{dias}d ";

            if (horas > 0)
                resultado += $"{horas}h ";

            if (minutos > 0)
                resultado += $"{minutos}min ";

            return resultado.Trim() + " atrás";
        }

    }
    }
