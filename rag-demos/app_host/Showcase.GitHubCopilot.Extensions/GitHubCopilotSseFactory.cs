using OpenAI.Chat;
using Showcase.Shared.AIExtensions;
using StreamingChatCompletionUpdate = OpenAI.Chat.StreamingChatCompletionUpdate;

namespace Showcase.GitHubCopilot.Extensions;
public static class GitHubCopilotSseFactory
{
    private const string _terminalData = "[DONE]";

    public static SseEvent<string> DoneEvent()
    {
        return new SseEvent<string>(_terminalData);
    }

    public static SseEvent<StreamingChatCompletionUpdate> AckEvent()
    {
        var message = OpenAIChatModelFactory.StreamingChatCompletionUpdate(role: ChatMessageRole.Assistant, contentUpdate: new(""));
        return new SseEvent<StreamingChatCompletionUpdate>(message);
    }

    public static SseEvent<CopilotConfirmationEvent> CopilotConfirmationEvent(string title, string message, string Id, Dictionary<string, object>? metadata = null)
    {
        var confirmation = metadata ?? new Dictionary<string, object>();
        confirmation["id"] = Id;
        return new SseEvent<CopilotConfirmationEvent>(new CopilotConfirmationEvent(title, message, confirmation))
        {
            EventType = "copilot_confirmation"
        };
    }

    public static SseEvent<CopilotReference[]> CopilotReferenceEvent(CopilotReference[] copilotReferences)
    {
        return new SseEvent<CopilotReference[]>(copilotReferences)
        {
            EventType = "copilot_references"
        };
    }

    public static SseEvent<CopilotError[]> CopilotErrorsEvent(CopilotError[] errors)
    {
        return new SseEvent<CopilotError[]>(errors)
        {
            EventType = "copilot_errors"
        };
    }

}
