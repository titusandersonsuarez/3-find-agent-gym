namespace LeadGym.AI.Configuration;

public class ApiSettings
{
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModelId { get; set; } = "gemini-1.5-flash";
    public string SerperApiKey { get; set; } = string.Empty;
}