using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace LeadGym.AI.Tools;

public class WebScraperPlugin
{
    private readonly HttpClient _httpClient;

    public WebScraperPlugin()
    {
        _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
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
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var websiteUri) ||
                (websiteUri.Scheme != Uri.UriSchemeHttp && websiteUri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(websiteUri.Host))
            {
                return "No se pudo verificar el sitio: la URL no es válida.";
            }

            if (await IsPrivateOrLocalHostAsync(websiteUri))
            {
                return "No se pudo verificar el sitio: la dirección no es pública.";
            }

            using var response = await _httpClient.GetAsync(websiteUri);
            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
            {
                return "No se pudo verificar el sitio: requiere una redirección no permitida.";
            }

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            
            // Limpieza básica de HTML para extraer solo el texto relevante
            string plainText = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            plainText = Regex.Replace(plainText, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            plainText = Regex.Replace(plainText, @"<[^>]+>", " ");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            // Retornamos un fragmento suficiente para el análisis del LLM
            return plainText.Length > 2000 ? plainText.Substring(0, 2000) : plainText;
        }
        catch (HttpRequestException)
        {
            return "No se pudo verificar el sitio por un error de conexión.";
        }
        catch (TaskCanceledException)
        {
            return "No se pudo verificar el sitio porque tardó demasiado en responder.";
        }
    }

    private static async Task<bool> IsPrivateOrLocalHostAsync(Uri uri)
    {
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var addresses = IPAddress.TryParse(uri.Host, out var address)
            ? new[] { address }
            : await Dns.GetHostAddressesAsync(uri.DnsSafeHost);

        return addresses.Any(IsPrivateAddress);
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254);
        }

        var ipv6 = address.GetAddressBytes();
        return (ipv6[0] & 0xfe) == 0xfc || (ipv6[0] == 0xfe && (ipv6[1] & 0xc0) == 0x80);
    }
}