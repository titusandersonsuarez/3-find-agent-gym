using System.Text.Json.Serialization;

namespace LeadGym.AI.Models;

public class EmailDraft
{
    [JsonPropertyName("prospect")]
    public GymProspect Prospect { get; set; } = new();

    [JsonPropertyName("auditReport")]
    public string AuditReport { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pendiente";

    [JsonPropertyName("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; }
}