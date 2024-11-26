using System.Text.Json.Serialization;

namespace Showcase.GitHubCopilot.Extensions.Models;
public class GitHubCopilotMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("copilot_references")]
    public List<GitHubCopilotReferenceJsonData> CopilotReferences { get; set; } = [];

    [JsonPropertyName("copilot_confirmations")]
    public List<GitHubCopilotConfirmation> CopilotConfirmations { get; set; } = [];
}
