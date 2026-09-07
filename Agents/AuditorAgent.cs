#pragma warning disable SKEXP0070

using LeadGym.AI.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LeadGym.AI.Agents;

public class AuditorAgent
{
    private readonly Kernel _kernel;

    public AuditorAgent(string apiKey, string modelId, bool isGemini = true)
    {
        var builder = Kernel.CreateBuilder();

        if (isGemini)
        {
            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
        }
        else
        {
            builder.AddOpenAIChatCompletion(modelId, apiKey);
        }

        // Inyectamos el plugin de Scraping
        builder.Plugins.AddFromObject(new WebScraperPlugin(), nameof(WebScraperPlugin));

        _kernel = builder.Build();
    }

    public async Task<string> AnalyzeWebsiteAsync(string gymName, string websiteUrl)
    {
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage(
            "Eres un Auditor Senior de Presencia Digital y Sistemas Web. " +
            "Tu trabajo es analizar el sitio web o estado digital de un gimnasio usando la herramienta 'ScrapeWebsiteTextAsync'.\n\n" +
            "Debes emitir un reporte conciso evaluando:\n" +
            "1. Diagnóstico del sitio (¿Existe?, ¿Carga correctamente?).\n" +
            "2. Evaluación de Agendamiento (¿Tiene botón o sistema para agendar/inscribirse en línea o usa WhatsApp/manual?).\n" +
            "3. Puntos débiles detectados (Diseño antiguo, falta de precios, sin llamado a la acción claro).\n" +
            "4. Veredicto Comercial (¿Es un buen candidato para ofrecerle una plataforma de agendamiento o un Bot conversacional? Responder SÍ/NO y por qué)."
        );

        history.AddUserMessage($"Por favor analiza la presencia digital del gimnasio '{gymName}' con la URL: {websiteUrl}");

        var settings = new GeminiPromptExecutionSettings
        {
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
        };

        var result = await chatCompletion.GetChatMessageContentAsync(history, settings, _kernel);
        return result.Content ?? "No se pudo realizar el análisis.";
    }
}
#pragma warning restore SKEXP0070