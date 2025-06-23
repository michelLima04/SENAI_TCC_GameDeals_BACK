using HtmlAgilityPack;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

public class ScraperService
{
    public async Task<(string titulo, string imagemUrl, decimal preco, string siteVendedor, List<string> falhas)>
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
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            var html = await httpClient.GetStringAsync(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            if (!apenasPreco)
            {
                // Site vendedor (domínio)
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

                // Título
                var tituloNode = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='productTitle']");
                if (tituloNode != null)
                {
                    titulo = WebUtility.HtmlDecode(tituloNode.InnerText.Trim());
                }
                else
                {
                    falhas.Add("Não foi possível extrair o título.");
                }

                // Imagem
                var imagemNode = htmlDoc.DocumentNode.SelectSingleNode("//img[@id='landingImage']")
                                 ?? htmlDoc.DocumentNode.SelectSingleNode("//img[@data-old-hires]");
                if (imagemNode != null)
                {
                    imagemUrl = imagemNode.GetAttributeValue("src", null);
                }
                else
                {
                    falhas.Add("Não foi possível extrair a imagem.");
                }
            }

            // Preço
            string precoTexto = null;
            var precoNode = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='a-offscreen']")
                          ?? htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class,'a-price-whole')]");

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
}