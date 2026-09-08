#pragma warning disable SKEXP0070

using System.Text.Json;
using LeadGym.AI.Tools;
using LeadGym.AI.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LeadGym.AI.Agents;

public class ProspectorAgent
{
    private readonly Kernel _kernel;
    private readonly string _serperApiKey;

    public ProspectorAgent(string geminiApiKey, string modelId, string serperApiKey)
    {
        _serperApiKey = serperApiKey;
        // 1. Configurar el Kernel con Gemini
        var builder = Kernel.CreateBuilder();
        builder.AddGoogleAIGeminiChatCompletion(modelId, geminiApiKey);

        _kernel = builder.Build();
    }

    public async Task<List<GymProspect>> SearchGymsAsync(string location)
    {
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // System Prompt que define el Rol y Meta del Agente
        var history = new ChatHistory();
        history.AddSystemMessage(
            "Eres un Especialista en Búsqueda y Prospección B2B de Gimnasios. " +
            "Tu objetivo es usar la herramienta 'SearchGoogleAsync' para encontrar gimnasios y centros de fitness locales " +
            "en la ubicación especificada. Debes extraer una lista detallada que incluya: " +
            "1. Nombre del gimnasio\n" +
            "2. Sitio web (si tiene o indicar 'No tiene')\n" +
            "3. Breve descripción de sus servicios o presencia digital visualizada.\n\n" +
            "RESPONDE EXCLUSIVAMENTE con un array JSON válido, sin markdown ni texto adicional. " +
            "Usa exactamente este JSON Schema: " + GymProspect.JsonSchema +
            " Si no existe un sitio web, usa null en websiteUrl. No agregues propiedades, " +
            "no uses comentarios y no inventes datos."
        );

        var searchPlugin = new SerperSearchPlugin(_serperApiKey);
        var searchResults = await searchPlugin.SearchGoogleAsync($"Gimnasios en {location}");
        history.AddUserMessage(
            $"Analiza estos resultados reales de búsqueda para encontrar 5 gimnasios en {location}. " +
            "No inventes datos y presenta nombre, sitio web y descripción:\n\n" + searchResults);

        ChatMessageContent? result = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var settings = new GeminiPromptExecutionSettings
                {
                    Temperature = 0
                };
                result = await chatCompletion.GetChatMessageContentAsync(history, settings, _kernel);
                break;
            }
            catch (Exception ex) when (attempt < 3 && (ex.Message.Contains("503") || ex.Message.Contains("ServiceUnavailable")))
            {
                Console.WriteLine($"[!] Gemini está ocupado. Reintentando Prospector ({attempt}/3)...");
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }

        if (result is null)
            throw new InvalidOperationException("Gemini no pudo responder al Prospector después de varios intentos.");

        return ParseProspects(GetFullContent(result));
    }

    private static string GetFullContent(ChatMessageContent message)
    {
        return string.Join(Environment.NewLine,
            message.Items.OfType<TextContent>().Select(item => item.Text));
    }

    private static List<GymProspect> ParseProspects(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("El Prospector devolvió una respuesta vacía.");

        var json = content.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineBreak = json.IndexOf('\n');
            var closingFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineBreak < 0 || closingFence <= firstLineBreak)
                throw new InvalidOperationException("El Prospector devolvió un bloque JSON incompleto.");

            json = json[(firstLineBreak + 1)..closingFence].Trim();
        }

        try
        {
            var prospects = JsonSerializer.Deserialize<List<GymProspect>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (prospects is null || prospects.Count == 0 || prospects.Any(prospect =>
                    string.IsNullOrWhiteSpace(prospect.Name) ||
                    string.IsNullOrWhiteSpace(prospect.Location) ||
                    string.IsNullOrWhiteSpace(prospect.InitialNotes)))
                throw new InvalidOperationException("El Prospector devolvió una lista vacía o con campos obligatorios incompletos.");

            return prospects;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("El Prospector no devolvió un array JSON válido.", ex);
        }
    }
}
#pragma warning restore SKEXP0070