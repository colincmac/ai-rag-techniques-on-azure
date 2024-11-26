using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;

namespace Showcase.Shared.AIExtensions.Realtime;
public class OutputWebSocketHandler : IOutputWebSocket
{
    private readonly ILogger? _logger;
    public WebSocket WebSocket;
    private readonly ArrayPool<byte> _arrayPool;
    private const int DefaultReceiveBufferSize = 1024 * 2;
    private int _receiveBufferSize;
    private readonly SemaphoreSlim _clientSendSemaphore = new(initialCount: 1, maxCount: 1);

#pragma warning disable OPENAI002
    public OutputWebSocketHandler(
        WebSocket webSocket,
        int? receiveBufferSize,
        ILogger? logger)
    {
        WebSocket = webSocket;
        _logger = logger;
        _receiveBufferSize = receiveBufferSize ?? DefaultReceiveBufferSize;
        _arrayPool = ArrayPool<byte>.Shared;
    }

    public virtual BinaryData ConvertAudioToRequestData(byte[] audioIn)
    {
        return BinaryData.FromBytes(audioIn);
    }
    public virtual BinaryData? TryGetAudioFromResponse(byte[] audioOut)
    {
        return new BinaryData(audioOut);
    }

    public async Task StartProcessingWebSocket(IVoiceClient voiceClient, CancellationToken cancellationToken = default)
    {
        try
        {
            while(WebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                await ReceiveUpdatesAsync(voiceClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception in WebSocket processing.");
        }
        finally
        {
            await DisposeAsync();
        }
    }

    public virtual async Task SendInputAudioAsync(byte[] audio, CancellationToken cancellationToken = default)
    {
            BinaryData requestData = ConvertAudioToRequestData(audio);
            await SendCommandAsync(requestData, cancellationToken);
    }

    public virtual async Task SendStopAudioCommand(CancellationToken cancellationToken = default)
    {
        var stopCommand = BinaryData.FromBytes("""
            {
                "kind": "StopAudio",
                "stopAudio": {}
            }
            """u8.ToArray());
        await SendCommandAsync(stopCommand, cancellationToken);
    }

    protected async Task SendCommandAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        ArraySegment<byte> messageBytes = new(data.ToArray());
        await _clientSendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WebSocket.SendAsync(
                messageBytes,
                WebSocketMessageType.Text, // TODO: extensibility for binary messages -- via "content"?
                endOfMessage: true,
                cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _clientSendSemaphore.Release();
        }
    }

    private async Task ReceiveUpdatesAsync(IVoiceClient voiceClient, CancellationToken cancellationToken)
    {
        var buffer = _arrayPool.Rent(_receiveBufferSize);

        try
        {

            var result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            var trimmed = buffer.Take(result.Count).ToArray();
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await DisposeAsync();
            }

            if (result.MessageType != WebSocketMessageType.Close && TryGetAudioFromResponse(trimmed) is BinaryData data)
            {
                await voiceClient.SendInputAudioAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("WebSocket receive operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error receiving WebSocket messages.");
        }
        finally
        {
            _arrayPool.Return(buffer);
        }
    }

    private async Task CloseWebSocketAsync()
    {
        if (WebSocket.State != WebSocketState.Closed)
        {
            try
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error closing WebSocket.");
            }
        }
    }
    public async ValueTask DisposeAsync()
    {
        await CloseWebSocketAsync();
        WebSocket.Dispose();
        GC.SuppressFinalize(this);
    }
}
