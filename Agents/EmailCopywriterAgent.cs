#pragma warning disable SKEXP0070

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LeadGym.AI.Agents;

public class EmailCopywriterAgent
{
    private readonly Kernel _kernel;

    public EmailCopywriterAgent(string geminiApiKey, string modelId)
    {
        var builder = Kernel.CreateBuilder();

        // Configuración nativa para Google Gemini
        builder.AddGoogleAIGeminiChatCompletion(modelId, geminiApiKey);

        _kernel = builder.Build();
    }

    public async Task<string> GenerateColdEmailAsync(string gymName, string auditReport)
    {
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage(
            "Eres un Copywriter B2B experto en Email Marketing y Estrategias de Ventas para Negocios de Fitness.\n" +
            "Tu objetivo es redactar un correo electrónico frío ultra-personalizado dirigido al dueño o gerente del gimnasio.\n\n" +
            "REGLAS DE REDACCIÓN:\n" +
            "1. Asunto relevante y corto (máximo 6-8 palabras) que genere apertura sin parecer spam.\n" +
            "2. Estructura:\n" +
            "   - Gancho/Elogio honesto sobre su gimnasio.\n" +
            "   - Mención específica de un problema encontrado en su auditoría digital (ej. falta de agendamiento 24/7 o respuesta lenta en WhatsApp).\n" +
            "   - Propuesta de solución breve: Sistema de reservas web o Agente Conversacional con IA que inscribe clientes automáticamente.\n" +
            "   - Llamado a la acción (CTA) suave e irresistible para coordinar una llamada de 10 minutos o demo corta.\n" +
            "3. Tono: Profesional, empático, directo y enfocado en aumentar sus ventas/afiliaciones."
        );

        history.AddUserMessage(
            $"Redacta una propuesta de correo para el gimnasio '{gymName}'.\n\n" +
            $"Basado en este Reporte de Auditoría Digital:\n{auditReport}"
        );

        ChatMessageContent? result = null;
        var settings = new GeminiPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 2048
        };

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                result = await chatCompletion.GetChatMessageContentAsync(history, settings, _kernel);
                break;
            }
            catch (Exception ex) when (attempt < 3 && (ex.Message.Contains("503") || ex.Message.Contains("ServiceUnavailable")))
            {
                Console.WriteLine($"[!] Gemini está ocupado. Reintentando Copywriter ({attempt}/3)...");
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }

        if (result is null)
            throw new InvalidOperationException("Gemini no pudo responder al Copywriter después de varios intentos.");

        var content = GetFullContent(result);
        return string.IsNullOrWhiteSpace(content) ? "No se pudo generar la propuesta de correo." : content;
    }

    private static string GetFullContent(ChatMessageContent message)
    {
        return string.Join(Environment.NewLine,
            message.Items.OfType<TextContent>().Select(item => item.Text));
    }
}
#pragma warning restore SKEXP0070