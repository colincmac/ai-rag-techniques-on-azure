using Azure.Communication.CallAutomation;
using Microsoft.Extensions.AI;
using Showcase.Shared.AIExtensions;
using Showcase.Shared.AIExtensions.Realtime;
using System.Net.WebSockets;
using System.Text;

namespace Showcase.VoiceRagAgent;


public class AcsAIOutboundHandler : OutputWebSocketHandler
{
    private const int DefaultReceiveBufferSize = 1024 * 16;

    public AcsAIOutboundHandler(WebSocket webSocket, int? receiveBufferSize = DefaultReceiveBufferSize, ILogger? logger = default) : base(webSocket, receiveBufferSize, logger)
    {
    }

    public override BinaryData ConvertAudioToRequestData(byte[] audioIn)
    {
        var audio = OutStreamingData.GetAudioDataForOutbound(audioIn);
        return BinaryData.FromString(audio);
    }

    public override BinaryData? TryGetAudioFromResponse(byte[] audioOut)
    {
        string data = Encoding.UTF8.GetString(audioOut).TrimEnd('\0');

        var input = StreamingData.Parse(data);
        return input is AudioData audioData ? new BinaryData(audioData.Data) : null;
    }

    public override async Task SendStopAudioCommand(CancellationToken cancellationToken = default)
    {
        var response = OutStreamingData.GetStopAudioForOutbound();
        await SendCommandAsync(BinaryData.FromString(response), cancellationToken);
    }
}
