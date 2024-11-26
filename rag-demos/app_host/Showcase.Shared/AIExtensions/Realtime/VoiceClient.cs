using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime.AudioDataProvider;
using System;
using System.Buffers;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;

namespace Showcase.Shared.AIExtensions.Realtime;

#pragma warning disable OPENAI002

public class VoiceClient : IVoiceClient
{
    private readonly ILogger _logger;
    private readonly RealtimeConversationClient _aiClient;
    private readonly Channel<ReadOnlyMemory<byte>> _audioInboundChannel;
    private readonly CancellationTokenSource _cts = new ();
    private RealtimeConversationSession? _aiSession;
    private readonly ConcurrentDictionary<Type, List<Func<IOutputWebSocket, ConversationUpdate, Task>>> _eventHandlers
        = new ();

    public VoiceClient(AzureOpenAIClient aiClient, string modelId, ILogger logger)
    {
        _aiClient = aiClient.GetRealtimeConversationClient(modelId);
        _logger = logger;
        _audioInboundChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });
       
    }

    /// <summary>
    /// Registers a handler for a specific type of ConversationUpdate.
    /// </summary>
    /// <typeparam name="T">The type of ConversationUpdate to handle.</typeparam>
    /// <param name="handler">The handler to invoke when the specified ConversationUpdate is received.</param>
    /// <returns>The current instance of <see cref="IVoiceClient"/>.</returns>
    public IVoiceClient OnConversationUpdate<T>(ConversationUpdateHandler<T> handler) where T : ConversationUpdate
    {
        ArgumentNullException.ThrowIfNull(handler);

        // Wrap the handler to match the Func signature
        Func<IOutputWebSocket, ConversationUpdate, Task> wrappedHandler = async (output, update) =>
        {
            if (update is T typedUpdate)
            {
                await handler(output, typedUpdate).ConfigureAwait(false);
            }
        };

        // Add the wrapped handler to the list for the specific type
        var handlers = _eventHandlers.GetOrAdd(typeof(T), _ => new List<Func<IOutputWebSocket, ConversationUpdate, Task>>());
        lock (handlers) // Ensure thread-safe addition
        {
            handlers.Add(wrappedHandler);
        }

        return this;
    }

    public async Task StartConversationAsync(IOutputWebSocket output, ConversationSessionOptions sessionOptions, CancellationToken cancellationToken = default)
    {
        _aiSession = await GetOrCreateSessionAsync(sessionOptions).ConfigureAwait(false);

        var processAudioTask = Task.Run(async () => await ProcessAudioAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
        var getOpenAiStreamResponseTask = Task.Run(async () => await GetOpenAiStreamResponseAsync(output, cancellationToken).ConfigureAwait(false), cancellationToken);

        await output.StartProcessingWebSocket(this, cancellationToken).ConfigureAwait(false);

        // Optionally, you can await the other tasks if you need to ensure they complete
        // await Task.WhenAll(processAudioTask, getOpenAiStreamResponseTask).ConfigureAwait(false);
    }
  
    public async Task SendInputAudioAsync(BinaryData audio, CancellationToken cancellationToken = default)
    {
        try
        {
            await _audioInboundChannel.Writer.WriteAsync(audio.ToArray(), cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning("Attempted to write to a closed channel.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing audio data to the channel.");
            throw;
        }
    }

    private async Task<RealtimeConversationSession> GetOrCreateSessionAsync(ConversationSessionOptions? sessionOptions = default)
    {
        if (_aiSession != null) return _aiSession;

        _aiSession = await _aiClient.StartConversationSessionAsync();
        await _aiSession.ConfigureSessionAsync(sessionOptions);
        return _aiSession;
    }

    private async Task GetOpenAiStreamResponseAsync(IOutputWebSocket mediaStreamingHandler, CancellationToken cancellationToken = default)
    {
        var aiSession = await GetOrCreateSessionAsync();
        try
        {
            await aiSession.StartResponseAsync(cancellationToken);
            await foreach (var update in aiSession.ReceiveUpdatesAsync(cancellationToken))
            {
                await HandleUpdateAsync(mediaStreamingHandler, update, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AI streaming was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during AI streaming.");
        }
    }

    private async Task ProcessAudioAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var audioChunk in _audioInboundChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await HandleAudioChunkAsync(audioChunk, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio processing was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data.");
            // Consider implementing retry logic or other recovery mechanisms
        }
    }
    private async Task HandleAudioChunkAsync(ReadOnlyMemory<byte> audioChunk, CancellationToken cancellationToken)
    {
        if (_aiSession == null)
        {
            _logger.LogWarning("AI session is not initialized.");
            return;
        }

        try
        {
            using var audioStream = new MemoryStream(audioChunk.ToArray());
            await _aiSession.SendInputAudioAsync(audioStream, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Sent audio chunk of size {ChunkSize} bytes to AI session.", audioChunk.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio chunk to AI session.");
        }
    }


    private async Task HandleUpdateAsync(IOutputWebSocket mediaStreamingHandler, ConversationUpdate update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        // Invoke registered handlers
        if (_eventHandlers.TryGetValue(update.GetType(), out var handlers))
        {
            // Create a copy to prevent issues if handlers are modified during iteration
            List<Func<IOutputWebSocket, ConversationUpdate, Task>> handlersCopy;
            lock (handlers)
            {
                handlersCopy = [.. handlers];
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    await handler.Invoke(mediaStreamingHandler, update).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing handler for {UpdateType}", update.GetType().Name);
                }
            }
        }
        
        if(update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
        {
            _logger.LogDebug("Delta Audio Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
            _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);
            _logger.LogDebug("Delta Function Args: {FunctionArguments}", deltaUpdate.FunctionArguments);
            if (deltaUpdate.AudioBytes is not null)
                await mediaStreamingHandler.SendInputAudioAsync(deltaUpdate.AudioBytes.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        if(update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
        {
            _logger.LogDebug($"Voice activity detection started at {speechStartedUpdate.AudioStartTime} ms");
            await mediaStreamingHandler.SendStopAudioCommand(cancellationToken).ConfigureAwait(false);
        }
        //switch (update)
        //{
        //    case ConversationSessionStartedUpdate sessionStartedUpdate:
        //        _logger.LogInformation("Session started. ID: {SessionId}", sessionStartedUpdate.SessionId);
        //        break;

        //    case ConversationInputSpeechStartedUpdate speechStartedUpdate:
        //        _logger.LogInformation("Voice activity detection started at {AudioStartTime} ms", speechStartedUpdate.AudioStartTime);
        //        await mediaStreamingHandler.SendStopAudioCommand(cancellationToken).ConfigureAwait(false);
        //        break;

        //    case ConversationInputSpeechFinishedUpdate speechFinishedUpdate:
        //        _logger.LogInformation("Voice activity detection ended at {AudioEndTime} ms", speechFinishedUpdate.AudioEndTime);
        //        break;

        //    case ConversationItemStreamingPartDeltaUpdate deltaUpdate:
        //        _logger.LogDebug("Delta Audio Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
        //        _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);
        //        _logger.LogDebug("Delta Function Args: {FunctionArguments}", deltaUpdate.FunctionArguments);
        //        if (deltaUpdate.AudioBytes is not null)
        //            await mediaStreamingHandler.SendInputAudioAsync(deltaUpdate.AudioBytes.ToArray(), cancellationToken).ConfigureAwait(false);
        //        break;

        //    case ConversationItemStreamingStartedUpdate itemStartedUpdate:
        //        _logger.LogInformation("Begin streaming of new item");
        //        break;

        //    case ConversationItemStreamingFinishedUpdate itemFinishedUpdate:
        //        _logger.LogInformation("Item streaming finished, response_id={ResponseId}", itemFinishedUpdate.ResponseId);
        //        break;

        //    case ConversationInputTranscriptionFinishedUpdate transcriptionCompletedUpdate:
        //        _logger.LogInformation("User audio transcript: {Transcript}", transcriptionCompletedUpdate.Transcript);
        //        break;

        //    case ConversationResponseFinishedUpdate turnFinishedUpdate:
        //        _logger.LogInformation("Model turn generation finished. Status: {Status}", turnFinishedUpdate.Status);
        //        break;

        //    case ConversationErrorUpdate errorUpdate:
        //        _logger.LogError("ERROR: {ErrorCode} {Message}", errorUpdate.ErrorCode, errorUpdate.Message);
        //        break;

        //    default:
        //        _logger.LogWarning("Unhandled conversation update type: {UpdateType}", update.GetType().Name);
        //        break;
        //}
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _audioInboundChannel.Writer.TryComplete();
        _aiSession?.Dispose();
        _eventHandlers.Clear();
        GC.SuppressFinalize(this);
    }
}
