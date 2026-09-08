using System.Text.Json.Serialization;

namespace LeadGym.AI.Models;

public class GymProspect
{
        public const string JsonSchema = """
                {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "websiteUrl": { "type": ["string", "null"] },
                            "location": { "type": "string" },
                            "initialNotes": { "type": "string" }
                        },
                        "required": ["name", "websiteUrl", "location", "initialNotes"],
                        "additionalProperties": false
                    }
                }
                """;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("initialNotes")]
    public string InitialNotes { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasWebsite => !string.IsNullOrWhiteSpace(WebsiteUrl) && !WebsiteUrl.Equals("No tiene", StringComparison.OrdinalIgnoreCase);
}