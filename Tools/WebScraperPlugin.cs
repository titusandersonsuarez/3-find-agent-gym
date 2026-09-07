using System.ComponentModel;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace LeadGym.AI.Tools;

public class WebScraperPlugin
{
    private readonly HttpClient _httpClient;

    public WebScraperPlugin()
    {
        _httpClient = new HttpClient();
        // Configuramos un User-Agent para evitar bloqueos básicos
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    [KernelFunction, Description("Visita un sitio web y extrae el texto principal para analizar si tiene sistema de reservas o estado del sitio.")]
    public async Task<string> ScrapeWebsiteTextAsync(
        [Description("La URL del sitio web del gimnasio a analizar (ej. https://gimnasio.com)")] string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Equals("No tiene", StringComparison.OrdinalIgnoreCase))
        {
            return "El gimnasio no cuenta con un sitio web registrado.";
        }

        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            var html = await _httpClient.GetStringAsync(url);
            
            // Limpieza básica de HTML para extraer solo el texto relevante
            string plainText = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            plainText = Regex.Replace(plainText, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            plainText = Regex.Replace(plainText, @"<[^>]+>", " ");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            // Retornamos un fragmento suficiente para el análisis del LLM
            return plainText.Length > 2000 ? plainText.Substring(0, 2000) : plainText;
        }
        catch (Exception ex)
        {
            return $"Error al intentar acceder al sitio web ({url}): {ex.Message}";
        }
    }
}