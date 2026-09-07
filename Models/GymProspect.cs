namespace LeadGym.AI.Models;

public class GymProspect
{
    public string Name { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string Location { get; set; } = string.Empty;
    public string InitialNotes { get; set; } = string.Empty;
    public bool HasWebsite => !string.IsNullOrWhiteSpace(WebsiteUrl) && !WebsiteUrl.Equals("No tiene", StringComparison.OrdinalIgnoreCase);
}