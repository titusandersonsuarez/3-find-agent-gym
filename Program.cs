using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using LeadGym.AI.Models;
using LeadGym.AI.Agents;
using LeadGym.AI.Configuration;
using Microsoft.Extensions.Configuration;

async Task<string> ValidateGeminiAccessAsync(string apiKey, string modelId)
{
    if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("TU_GEMINI", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La clave de Gemini no está configurada. Revisa 'ApiSettings:GeminiApiKey' en appsettings.json.");
    }

    if (string.IsNullOrWhiteSpace(modelId))
    {
        throw new InvalidOperationException("El modelo de Gemini no está configurado. Revisa 'ApiSettings:GeminiModelId' en appsettings.json.");
    }

    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var modelsToTry = new[] { modelId }.Distinct(StringComparer.OrdinalIgnoreCase);
    HttpRequestException? lastError = null;

    foreach (var candidateModel in modelsToTry)
    {
        var uri = $"https://generativelanguage.googleapis.com/v1beta/models/{candidateModel}:generateContent?key={apiKey}";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await client.PostAsJsonAsync(uri, new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = "Prueba de conexión" }
                            }
                        }
                    }
                });

                if (response.IsSuccessStatusCode)
                {
                    return candidateModel;
                }

                var body = await response.Content.ReadAsStringAsync();
                lastError = new HttpRequestException(
                    $"Gemini respondió con {(int)response.StatusCode} ({response.StatusCode}) usando '{candidateModel}'. " +
                    $"Detalle: {body}",
                    null,
                    response.StatusCode);

                if (response.StatusCode != HttpStatusCode.ServiceUnavailable && response.StatusCode != HttpStatusCode.TooManyRequests)
                {
                    break;
                }

                if (attempt < 3)
                {
                    Console.WriteLine($"[!] Gemini está ocupado con '{candidateModel}'. Reintentando ({attempt}/3)...");
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
                var isPermanentError = ex.StatusCode.HasValue &&
                    ex.StatusCode != HttpStatusCode.ServiceUnavailable &&
                    ex.StatusCode != HttpStatusCode.TooManyRequests;

                if (isPermanentError)
                {
                    break;
                }

                if (attempt < 3)
                {
                    Console.WriteLine($"[!] Error temporal de Gemini. Reintentando ({attempt}/3)...");
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }
            catch (TaskCanceledException ex)
            {
                lastError = new HttpRequestException("Gemini tardó demasiado en responder.", ex);
                if (attempt < 3)
                {
                    Console.WriteLine($"[!] Gemini tardó demasiado. Reintentando ({attempt}/3)...");
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("No se pudo validar la conexión con Gemini. Revisa la clave y el modelo configurados.", ex);
            }
        }
    }

    throw lastError ?? new HttpRequestException("Gemini no respondió durante la validación.");
}

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

string MaskSecret(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "No configurada";

    if (value.Length <= 8)
        return "********";

    return value.Substring(0, 4) + "********" + value.Substring(value.Length - 4);
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

async Task SendDraftsEmailAsync(string draftsPath, int draftsCount, EmailSettings settings)
{
    if (string.IsNullOrWhiteSpace(settings.Username) ||
        string.IsNullOrWhiteSpace(settings.Password) ||
        settings.Password.Contains("TU_", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("[!] EmailSettings no está configurado. Los borradores quedaron guardados localmente, pero no se enviaron.");
        return;
    }

    var sender = string.IsNullOrWhiteSpace(settings.From) ? settings.Username : settings.From;
    using var message = new MailMessage(sender, settings.To)
    {
        Subject = $"LeadGym AI: {draftsCount} borradores pendientes de revisión",
        Body = "Se adjunta el archivo JSON con los borradores personalizados generados por LeadGym AI. " +
               "Todos están en estado Pendiente y no se han enviado a los gimnasios."
    };
    message.Attachments.Add(new Attachment(draftsPath, "application/json"));

    using var smtpClient = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
    {
        EnableSsl = true,
        Credentials = new NetworkCredential(settings.Username, settings.Password)
    };

    await smtpClient.SendMailAsync(message);
    Console.WriteLine($"[+] Borradores enviados a {settings.To}.");
}

Console.WriteLine("==============================================");
Console.WriteLine("    LEADGYM AI - Sistema Agéntico (C#)        ");
Console.WriteLine("==============================================\n");

try
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .Build();

    var apiSettings = configuration.GetSection("ApiSettings").Get<ApiSettings>()
        ?? throw new InvalidOperationException("No se pudo cargar la sección 'ApiSettings' desde appsettings.json.");
    var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();

    Console.WriteLine("[+] Configuración cargada:");
    Console.WriteLine($"    - Modelo Gemini: {apiSettings.GeminiModelId}");
    Console.WriteLine($"    - Clave Gemini: {MaskSecret(apiSettings.GeminiApiKey)}");
    Console.WriteLine($"    - Clave Serper: {MaskSecret(apiSettings.SerperApiKey)}");
    Console.WriteLine("    - Archivo: appsettings.json");
    Console.WriteLine();

    if (string.IsNullOrWhiteSpace(apiSettings.GeminiApiKey) || apiSettings.GeminiApiKey.Contains("TU_GEMINI", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La clave de Gemini no está configurada. Revisa 'ApiSettings:GeminiApiKey' en appsettings.json.");
    }

    if (string.IsNullOrWhiteSpace(apiSettings.SerperApiKey) || apiSettings.SerperApiKey.Contains("TU_SERPER", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La clave de Serper no está configurada. Revisa 'ApiSettings:SerperApiKey' en appsettings.json.");
    }

    Console.WriteLine("[+] Validando acceso a Gemini...");
    var activeModelId = await ValidateGeminiAccessAsync(apiSettings.GeminiApiKey, apiSettings.GeminiModelId);
    if (!string.Equals(activeModelId, apiSettings.GeminiModelId, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[+] Modelo alternativo activo: {activeModelId}");
    }

    Console.WriteLine("[+] Inicializando Agente 1 (Prospector)...");
    var prospector = new ProspectorAgent(apiSettings.GeminiApiKey, activeModelId, apiSettings.SerperApiKey);

    Console.WriteLine("[+] Inicializando Agente 2 (Auditor Digital)...");
    var auditor = new AuditorAgent(apiSettings.GeminiApiKey, activeModelId);

    Console.WriteLine("[+] Inicializando Agente 3 (Email Copywriter)...");
    var copywriter = new EmailCopywriterAgent(apiSettings.GeminiApiKey, activeModelId);

    var targetCity = string.IsNullOrWhiteSpace(apiSettings.TargetCity) ? "Bucaramanga" : apiSettings.TargetCity;
    var maxProspects = Math.Clamp(apiSettings.MaxProspects, 1, 20);

    Console.WriteLine($"\n[+] PASO 1: Buscando gimnasios en {targetCity}...");
    List<GymProspect> prospects = (await prospector.SearchGymsAsync(targetCity))
        .Take(maxProspects)
        .ToList();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n--- RESULTADO DEL AGENTE 1 (PROSPECTOR) ---");
    Console.ResetColor();
    Console.WriteLine($"Se encontraron {prospects.Count} prospectos.");

    var draftsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
    Directory.CreateDirectory(draftsDirectory);
    var draftsPath = Path.Combine(draftsDirectory, $"email-drafts-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
    var emailDrafts = new List<EmailDraft>();
    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    void SaveDrafts()
    {
        File.WriteAllText(draftsPath, JsonSerializer.Serialize(emailDrafts, jsonOptions));
    }

    foreach (var prospect in prospects)
    {
        Console.WriteLine("\n------------------------------------------------");
        Console.WriteLine($"[+] PROSPECTO: {prospect.Name} ({prospect.Location})");

        string auditReport;
        if (prospect.HasWebsite)
        {
            Console.WriteLine("[+] PASO 2: Auditando presencia digital...");
            auditReport = await auditor.AnalyzeWebsiteAsync(prospect.Name, prospect.WebsiteUrl!);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n--- RESULTADO DEL AGENTE 2 (AUDITOR) ---");
            Console.ResetColor();
            Console.WriteLine(auditReport);
        }
        else
        {
            auditReport = "El gimnasio no tiene un sitio web identificado. Evaluar una propuesta de presencia digital y agendamiento desde cero.";
            Console.WriteLine("[!] Sin sitio web: se omite la auditoría y se continúa con una oportunidad de presencia digital.");
        }

        Console.WriteLine("\n[+] PASO 3: Redactando propuesta comercial personalizada...");
        string coldEmail = await copywriter.GenerateColdEmailAsync(prospect.Name, auditReport);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n--- RESULTADO DEL AGENTE 3 (EMAIL COPYWRITER) ---");
        Console.ResetColor();
        Console.WriteLine(coldEmail);

        emailDrafts.Add(new EmailDraft
        {
            Prospect = prospect,
            AuditReport = auditReport,
            Email = coldEmail,
            Status = "Pendiente",
            GeneratedAtUtc = DateTime.UtcNow
        });
        SaveDrafts();
        Console.WriteLine($"[+] Borrador guardado en: {draftsPath}");
    }

    Console.WriteLine($"\n[+] Se guardaron {emailDrafts.Count} borradores para revisión manual.");
    await SendDraftsEmailAsync(draftsPath, emailDrafts.Count, emailSettings);

    Console.WriteLine("\n==============================================");
    Console.WriteLine("    ¡Flujo completo ejecutado con éxito!      ");
    Console.WriteLine("==============================================");
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
        var isGeminiError = ex.Message.Contains("Gemini", StringComparison.OrdinalIgnoreCase);
        var title = isGeminiError ? "GEMINI NO DISPONIBLE (404)" : "API NO ENCONTRADA (404)";
        var message = isGeminiError
                ? "Gemini no pudo ejecutar generateContent con la clave o el modelo configurados.\n" +
                    "El modelo debe existir y la clave debe tener acceso a la API Generative Language.\n\n" +
                    "Revisa 'ApiSettings:GeminiApiKey' y 'ApiSettings:GeminiModelId' en appsettings.json.\n" +
                    $"Detalle técnico: {ex.Message}"
                : "La API externa no respondió en la URL esperada.\n" +
                    "Esto suele pasar por una URL incorrecta o una clave inválida.\n\n" +
                    $"Detalle técnico: {ex.Message}";

        PrintErrorBanner(title, message);
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
