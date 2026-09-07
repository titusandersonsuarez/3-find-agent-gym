using System.Net;
using LeadGym.AI.Agents;
using LeadGym.AI.Configuration;
using Microsoft.Extensions.Configuration;

void PrintErrorBanner(string title, string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"[ERROR] {title}");
    Console.WriteLine("==============================================");
    Console.WriteLine(message);
    Console.ResetColor();
}

string BuildFriendlyErrorMessage(Exception ex)
{
    var text = ex.Message ?? string.Empty;
    var inner = ex.InnerException?.Message ?? string.Empty;
    var combined = string.Join(" | ", new[] { text, inner }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());

    if (combined.Contains("404", StringComparison.OrdinalIgnoreCase) || combined.Contains("NotFound", StringComparison.OrdinalIgnoreCase) || combined.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
    {
        return "La API externa respondió con 404 (No encontrado).\n" +
               "Esto normalmente indica que la URL o el endpoint no existe, o que la llave no es válida para ese servicio.\n\n" +
               $"Detalle técnico: {combined}";
    }

    if (combined.Contains("401", StringComparison.OrdinalIgnoreCase) || combined.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
    {
        return "La API respondió con 401 (No autorizado).\n" +
               "La clave de acceso parece incorrecta, vencida o no tiene permisos para esa operación.\n\n" +
               $"Detalle técnico: {combined}";
    }

    if (combined.Contains("400", StringComparison.OrdinalIgnoreCase) || combined.Contains("BadRequest", StringComparison.OrdinalIgnoreCase) || combined.Contains("Bad Request", StringComparison.OrdinalIgnoreCase))
    {
        return "La API respondió con 400 (Solicitud incorrecta).\n" +
               "Revisa el formato de la petición, el modelo o los parámetros enviados.\n\n" +
               $"Detalle técnico: {combined}";
    }

    if (combined.Contains("429", StringComparison.OrdinalIgnoreCase) || combined.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase))
    {
        return "La API respondió con 429 (Demasiadas solicitudes).\n" +
               "Se excedió el límite de peticiones. Espera unos segundos e inténtalo de nuevo.\n\n" +
               $"Detalle técnico: {combined}";
    }

    if (combined.Contains("connection", StringComparison.OrdinalIgnoreCase) || combined.Contains("timeout", StringComparison.OrdinalIgnoreCase))
    {
        return "No se pudo establecer la conexión con la API externa.\n" +
               "Verifica tu acceso a internet, la clave y el estado del servicio.\n\n" +
               $"Detalle técnico: {combined}";
    }

    if (combined.Contains("API key", StringComparison.OrdinalIgnoreCase) || combined.Contains("key", StringComparison.OrdinalIgnoreCase) && combined.Contains("invalid", StringComparison.OrdinalIgnoreCase))
    {
        return "La clave de la API parece inválida o no está activa.\n" +
               "Revisa 'appsettings.json' y asegúrate de que la clave sea correcta y vigente.\n\n" +
               $"Detalle técnico: {combined}";
    }

    return $"Ocurrió un problema inesperado.\nDetalle técnico: {combined}";
}

Console.WriteLine("==============================================");
Console.WriteLine("    LEADGYM AI - Sistema Agéntico (C#)        ");
Console.WriteLine("==============================================\n");

try
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    var apiSettings = configuration.GetSection("ApiSettings").Get<ApiSettings>()
        ?? throw new InvalidOperationException("No se pudo cargar la sección 'ApiSettings' desde appsettings.json.");

    if (string.IsNullOrWhiteSpace(apiSettings.GeminiApiKey) || apiSettings.GeminiApiKey.Contains("TU_GEMINI"))
    {
        throw new InvalidOperationException("La clave de Gemini no está configurada. Revisa 'ApiSettings:GeminiApiKey' en appsettings.json.");
    }

    if (string.IsNullOrWhiteSpace(apiSettings.SerperApiKey) || apiSettings.SerperApiKey.Contains("TU_SERPER"))
    {
        throw new InvalidOperationException("La clave de Serper no está configurada. Revisa 'ApiSettings:SerperApiKey' en appsettings.json.");
    }

    Console.WriteLine("[+] Inicializando Agente 1 (Prospector)...");
    var prospector = new ProspectorAgent(apiSettings.GeminiApiKey, apiSettings.GeminiModelId, apiSettings.SerperApiKey);

    Console.WriteLine("[+] Inicializando Agente 2 (Auditor Digital)...");
    var auditor = new AuditorAgent(apiSettings.GeminiApiKey, apiSettings.GeminiModelId);

    Console.WriteLine("[+] Inicializando Agente 3 (Email Copywriter)...");
    var copywriter = new EmailCopywriterAgent(apiSettings.GeminiApiKey, apiSettings.GeminiModelId);

    string targetCity = "Bucaramanga";

    Console.WriteLine($"\n[+] PASO 1: Buscando gimnasios en {targetCity}...");
    string prospectResults = await prospector.SearchGymsAsync(targetCity);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n--- RESULTADO DEL AGENTE 1 (PROSPECTOR) ---");
    Console.ResetColor();
    Console.WriteLine(prospectResults);

    Console.WriteLine("\n------------------------------------------------");
    string testGymName = "Smart Body Gym";
    string testGymUrl = "https://www.google.com";

    Console.WriteLine($"[+] PASO 2: Auditando presencia digital de '{testGymName}'...");
    string auditReport = await auditor.AnalyzeWebsiteAsync(testGymName, testGymUrl);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\n--- RESULTADO DEL AGENTE 2 (AUDITOR) ---");
    Console.ResetColor();
    Console.WriteLine(auditReport);

    Console.WriteLine("\n------------------------------------------------");
    Console.WriteLine("[+] PASO 3: Redactando propuesta comercial personalizada...");
    string coldEmail = await copywriter.GenerateColdEmailAsync(testGymName, auditReport);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n--- RESULTADO DEL AGENTE 3 (EMAIL COPYWRITER) ---");
    Console.ResetColor();
    Console.WriteLine(coldEmail);

    Console.WriteLine("\n==============================================");
    Console.WriteLine("    ¡Flujo completo ejecutado con éxito!      ");
    Console.WriteLine("==============================================");
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    PrintErrorBanner("API NO ENCONTRADA (404)",
        "La API externa no respondió en la URL esperada.\n" +
        "Esto suele pasar por una URL incorrecta, una clave inválida o una API que no existe en ese endpoint.\n\n" +
        "Revisa la configuración de Serper y la URL del servicio.");
}
catch (HttpRequestException ex)
{
    PrintErrorBanner("ERROR DE CONEXIÓN A API",
        $"La llamada a una API falló con código HTTP {(int?)ex.StatusCode ?? 0}.\n" +
        $"Detalle: {ex.Message}\n\n" +
        "Verifica la clave, la red y que el servicio de terceros esté disponible.");
}
catch (InvalidOperationException ex)
{
    PrintErrorBanner("CONFIGURACIÓN INCORRECTA", ex.Message);
}
catch (Exception ex)
{
    var friendly = BuildFriendlyErrorMessage(ex);

    if (ex.ToString().Contains("Microsoft.SemanticKernel.Connectors.Google", StringComparison.OrdinalIgnoreCase) ||
        ex.ToString().Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase) ||
        ex.ToString().Contains("404", StringComparison.OrdinalIgnoreCase))
    {
        friendly = "La API de Gemini respondió con 404.\n" +
                   "Esto casi siempre significa que la clave de Google AI Studio es inválida, no está activada o el modelo no está disponible para esa cuenta.\n\n" +
                   "Verifica que tu clave de Gemini sea real y que el proyecto tenga acceso habilitado en Google AI Studio.\n\n" +
                   $"Detalle técnico: {ex.Message}";
    }

    PrintErrorBanner("ERROR DE GEMINI", friendly);
}

Console.WriteLine("\nPresiona cualquier tecla para finalizar...");
Console.ReadKey();