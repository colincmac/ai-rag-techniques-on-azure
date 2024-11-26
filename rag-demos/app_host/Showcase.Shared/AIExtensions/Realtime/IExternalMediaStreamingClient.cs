using OpenAI.RealtimeConversation;

namespace Showcase.Shared.AIExtensions.Realtime;

#pragma warning disable OPENAI002
public interface IOutputWebSocket : IAsyncDisposable
{
    Task StartProcessingWebSocket(IVoiceClient voiceClient, CancellationToken cancellationToken = default);
    Task SendInputAudioAsync(byte[] audio, CancellationToken token = default);
    BinaryData ConvertAudioToRequestData(byte[] audioIn);
    BinaryData? TryGetAudioFromResponse(byte[] audioOut);
    Task SendStopAudioCommand(CancellationToken cancellationToken = default);
}
