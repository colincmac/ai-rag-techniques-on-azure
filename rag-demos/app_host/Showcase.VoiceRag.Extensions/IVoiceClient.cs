using Microsoft.Extensions.AI;

namespace Showcase.VoiceRag.Extensions;

public interface IVoiceClient : IDisposable
{
    void StartConversation(ChatOptions? options = null, CancellationToken cancellationToken = default);
    Task SendAudioAsync(byte[] audioData);
    void Close();
}
