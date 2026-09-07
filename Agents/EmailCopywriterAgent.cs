#pragma warning disable SKEXP0070

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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

        var result = await chatCompletion.GetChatMessageContentAsync(history, kernel: _kernel);
        return result.Content ?? "No se pudo generar la propuesta de correo.";
    }
}
#pragma warning restore SKEXP0070