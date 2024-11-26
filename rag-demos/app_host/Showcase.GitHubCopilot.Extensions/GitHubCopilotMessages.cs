using Microsoft.Extensions.AI;
using System.Text.Json.Serialization;

namespace Showcase.GitHubCopilot.Extensions;

public record CopilotRepositoryReference(string Type, int Id, string Name, string Description, string Ref, string CommitOID);
public record CopilotChatMessage(string Role, string Content, [property: JsonPropertyName("copilot_references")] CopilotReference[]? CopilotReferences = null, [property: JsonPropertyName("copilot_confirmations")] object[]? CopilotConfirmations = null)
{
    public static implicit operator ChatMessage(CopilotChatMessage v) => new ChatMessage(new(v.Role), v.Content);
    //{
    //    ChatRequestMessage message = v.Role switch
    //    {
    //        "user" => new ChatRequestUserMessage(v.Content),
    //        "assistant" => new ChatRequestAssistantMessage(v.Content),
    //        "system" => new ChatRequestSystemMessage(v.Content),
    //        _ => throw new ArgumentException("Invalid role")
    //    };
    //    return message;
    //}
};

public record StreamingCopilotCompletionUpdate([property: JsonIgnore] StreamingChatCompletionUpdate update)
{

};

public record CopilotRequest([property: JsonPropertyName("copilot_thread_id")] string CopilotThreadId, CopilotChatMessage[] Messages);


public record CopilotConfirmationEvent(string Title, string Message, Dictionary<string, object> Confirmation)
{
    public string Type => "action";
};


public record CopilotError(string Identifier, string Message, string Code, CopilotErrorType Type);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CopilotErrorType
{
    reference,
    function,
    agent
}

public record CopilotReference(
  [property: JsonIgnore] string ReferenceType,
  [property: JsonIgnore] string ReferenceId,
  [property: JsonPropertyName("is_implicit")] bool IsImplicit = false,
  Dictionary<string, object>? Data = null,
  CopilotReferenceMetadata? Metadata = null
)
{
    public string Id { get; set; } = $"{ReferenceType}.reference.{ReferenceId}".ToLower();
    public string Type { get; set; } = $"{ReferenceType}.reference";
};

public record CopilotReferenceMetadata(
  [property: JsonPropertyName("display_name")] string DisplayName,
  [property: JsonPropertyName("display_icon")] string? DisplayIcon = null,
  [property: JsonPropertyName("display_url")] string? DisplayUrl = null
);