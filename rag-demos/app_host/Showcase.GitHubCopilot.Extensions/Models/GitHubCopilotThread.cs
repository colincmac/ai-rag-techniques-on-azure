using System.Text.Json.Serialization;

namespace Showcase.GitHubCopilot.Extensions.Models;
public class GitHubCopilotThread
{
    [JsonPropertyName("copilot_thread_id")]
    public required string Id { get; set; }

    [JsonPropertyName("messages")]
    public List<GitHubCopilotMessage> Messages { get; set; } = [];

    [JsonPropertyName("stop")]
    public object? Stop { get; set; }

    [JsonPropertyName("top_p")]
    public long TopP { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public long MaxTokens { get; set; }

    [JsonPropertyName("presence_penalty")]
    public long PresencePenalty { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public long FrequencyPenalty { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("copilot_skills")]
    public object? CopilotSkills { get; set; }

    [JsonPropertyName("copilot_contexts")]
    public object? CopilotContexts { get; set; }

    [JsonPropertyName("agent")]
    public string? Agent { get; set; }
}
