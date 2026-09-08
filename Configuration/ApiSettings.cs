namespace LeadGym.AI.Configuration;

public class ApiSettings
{
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModelId { get; set; } = "gemini-3.6-flash";
    public string SerperApiKey { get; set; } = string.Empty;
    public int MaxProspects { get; set; } = 5;
    public string TargetCity { get; set; } = "Bucaramanga";
}