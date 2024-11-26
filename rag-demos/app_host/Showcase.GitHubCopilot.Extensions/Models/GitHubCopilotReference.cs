using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Showcase.GitHubCopilot.Extensions.Models;

public interface IGitHubCopilotReference
{
    [JsonPropertyName("type")]
    string? Type { get; set; }

    [JsonPropertyName("id")]
    string? Id { get; set; }

    [JsonPropertyName("metadata")]
    CopilotReferenceMetadata? Metadata { get; set; }
}

public interface IGitHubCopilotReference<TData> : IGitHubCopilotReference
    where TData : IGitHubCopilotReferenceData
{
    [JsonPropertyName("data")]
    TData? Data { get; set; }
}

public interface IGitHubCopilotReferenceData
{
    [JsonPropertyName("type")]
    string Type { get; set; }
}

public class GitHubCopilotReference : IGitHubCopilotReferenceData
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("metadata")]
    public CopilotReferenceMetadata? Metadata { get; set; }

    [JsonPropertyName("is_implicit")]
    public bool IsImplicit { get; set; } = false;
}

public class GitHubCopilotReferenceJsonData : GitHubCopilotReference
{
    [JsonPropertyName("data")]
    public JsonObject? Data { get; set; }

}

public class GitHubCopilotReference<TData> : GitHubCopilotReference, IGitHubCopilotReference<TData>
    where TData : IGitHubCopilotReferenceData
{
    [JsonPropertyName("data")]
    public TData? Data { get; set; }
}

