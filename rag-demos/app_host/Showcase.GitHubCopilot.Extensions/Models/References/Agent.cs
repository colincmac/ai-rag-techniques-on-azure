using System.Text.Json.Serialization;

namespace Showcase.GitHubCopilot.Extensions.Models.References;


public class GitHubCopilotReferenceAgentData : IGitHubCopilotReferenceData
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("avatarURL")]
    public Uri? AvatarUrl { get; set; }
}

public class GitHubCopilotReferenceAgent : GitHubCopilotReference<GitHubCopilotReferenceAgentData>;