#pragma warning disable SKEXP0070

using LeadGym.AI.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LeadGym.AI.Agents;

public class ProspectorAgent
{
    private readonly Kernel _kernel;

    public ProspectorAgent(string geminiApiKey, string modelId, string serperApiKey)
    {
        // 1. Configurar el Kernel con Gemini
        var builder = Kernel.CreateBuilder();
        builder.AddGoogleAIGeminiChatCompletion(modelId, geminiApiKey);

        // 2. Registrar el Plugin de búsqueda en el Kernel
        builder.Plugins.AddFromObject(new SerperSearchPlugin(serperApiKey), nameof(SerperSearchPlugin));

        _kernel = builder.Build();
    }

    public async Task<string> SearchGymsAsync(string location)
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
            "3. Breve descripción de sus servicios o presencia digital visualizada."
        );

        history.AddUserMessage($"Encuentra 5 gimnasios o centros de fitness en la ciudad/zona de: {location}");

        // Configuración para permitir que el Agente decida ejecutar la herramienta automáticamente
        var settings = new GeminiPromptExecutionSettings
        {
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
        };

        var result = await chatCompletion.GetChatMessageContentAsync(history, settings, _kernel);
        return result.Content ?? "No se obtuvieron resultados.";
    }
}
#pragma warning restore SKEXP0070