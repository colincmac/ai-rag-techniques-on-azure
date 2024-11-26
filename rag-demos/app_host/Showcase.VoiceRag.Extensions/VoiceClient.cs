using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using System.Threading.Channels;

namespace Showcase.VoiceRag.Extensions;
#pragma warning disable OPENAI002

public class VoiceClient : IVoiceClient
{
    private readonly ILogger<VoiceClient> _logger;
    private readonly AzureOpenAIClient _aiClient;
    private readonly IAcsMediaStreamingClient _mediaStreaming;
    private readonly Channel<byte[]> _audioChannel;
    private readonly CancellationTokenSource _cts;
    private RealtimeConversationSession _aiSession;

    private const string DefaultSystemPrompt = @"You're an AI assistant for an elevator company called Contoso Elevators...";
    public VoiceClient(
    AzureOpenAIClient aiClient,
    IAcsMediaStreamingClient mediaStreaming,
    IConfiguration configuration,
    ILogger<AzureOpenAIService> logger)
    {
        _aiClient = aiClient;
        _mediaStreaming = mediaStreaming;
        _logger = logger;
        _audioChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });
        _cts = new CancellationTokenSource();
        InitializeAsync(configuration).GetAwaiter().GetResult();
        _ = Task.Run(ProcessAudioQueueAsync);
    }
}
