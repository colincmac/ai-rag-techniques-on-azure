using System.Text.Json.Serialization;

namespace Showcase.GitHubCopilot.Extensions.Models;

public enum ConfirmationType
{
    TemplateSelection,
    Fulfill
}
public enum ClientConfirmationState
{
    accepted,
    dismissed
}

public class GitHubCopilotConfirmation
{
    [JsonPropertyName("state")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClientConfirmationState State { get; set; } // accepted, dismissed

    [JsonPropertyName("confirmation")]
    public required GitHubCopilotConfirmationInfo Confirmation { get; set; }
}

public class GitHubCopilotConfirmationInfo
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("other")]
    public string? Other { get; set; } = "identifier-as-needed";
}



public class GitHubCopilotConfirmationEvent
{
    public required string Type { get; set; } = "action";
    public required string Title { get; set; }

    public string? Message { get; set; }

    public GitHubCopilotConfirmationInfo? Confirmation { get; set; }
}

public class GitHubCopilotConfirmationRequest
{
    public required string Id { get; set; }

    public required ConfirmationType Type { get; set; }

    public required string Title { get; set; }

    public string? Message { get; set; }

    public string? Other { get; set; }
}

