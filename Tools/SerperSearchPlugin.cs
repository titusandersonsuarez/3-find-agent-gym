using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel;

namespace LeadGym.AI.Tools;

public class SerperSearchPlugin
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public SerperSearchPlugin(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    [KernelFunction, Description("Busca información de negocios locales y gimnasios en Google mediante Serper API.")]
    public async Task<string> SearchGoogleAsync(
        [Description("La consulta de búsqueda para Google, por ejemplo: 'Gimnasios en Madrid'")] string query)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://google.serper.dev/search")
        {
            Headers = { { "X-API-KEY", _apiKey } },
            Content = JsonContent.Create(new { q = query })
        };

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Serper API respondió con {(int)response.StatusCode} ({response.StatusCode}). Detalle: {body}",
                null,
                response.StatusCode);
        }

        var jsonResult = await response.Content.ReadAsStringAsync();
        return jsonResult;
    }
}