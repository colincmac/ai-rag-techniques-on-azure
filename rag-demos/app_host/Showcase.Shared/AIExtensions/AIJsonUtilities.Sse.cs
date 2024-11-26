using Microsoft.Extensions.AI;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Showcase.Shared.AIExtensions;

// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.ServerSentEvents/src/System/Net/ServerSentEvents/SseItem.cs
// https://github.com/eiriktsarpalis/extensions/blob/ssewriter/src/Libraries/Microsoft.Extensions.AI/Utilities/AIJsonUtilities.Sse.cs
public static partial class AIJsonUtilitiesExtensions
{

    private static readonly byte[] _sseEventFieldPrefix = "event: "u8.ToArray();
    private static readonly byte[] _sseDataFieldPrefix = "data: "u8.ToArray();
    private static readonly byte[] _sseIdFieldPrefix = "id: "u8.ToArray();
    private static readonly byte[] _sseLineBreak = Encoding.UTF8.GetBytes("\n");

    /// <summary>
    /// Serializes the specified server-sent events to the provided stream as JSON data.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data payload in the event.</typeparam>
    /// <param name="stream">The UTF-8 stream to write the server-sent events to.</param>
    /// <param name="eventData">The data to convert to SSE and serialize to the stream.</param>
    /// <param name="options">The options configuring serialization.</param>
    /// <param name="cancellationToken">The token that can be used to cancel the write operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static ValueTask SerializeAndSendAsSseDataAsync<T>(
        this Stream stream,
        IAsyncEnumerable<T> eventData,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return SerializeAndSendSseAsync(stream, eventData.Select(x => new SseEvent<T>(x)), options, cancellationToken);
    }

    /// <summary>
    /// Serializes the specified server-sent events to the provided stream as JSON data.
    /// </summary>
    /// <typeparam name = "T" > Specifies the type of data payload in the event.</typeparam>
    /// <param name="stream">The UTF-8 stream to write the server-sent events to.</param>
    /// <param name="sseEvents">The events to serialize to the stream.</param>
    /// <param name="options">The options configuring serialization.</param>
    /// <param name="cancellationToken">The token that can be used to cancel the write operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static async ValueTask SerializeAndSendSseAsync<T>(
        this Stream stream,
        IAsyncEnumerable<SseEvent<T>> sseEvents,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        options ??= AIJsonUtilities.DefaultOptions;

        // Build a custom Utf8JsonWriter that ignores indentation configuration from JsonSerializerOptions.
        using Utf8JsonWriter writer = new(bufferWriter);
        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));

        await foreach (var sseEvent in sseEvents.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            JsonSerializer.Serialize(writer, sseEvent.Data, typeInfo);
            writer.Flush();

            await SendAsync(stream, sseEvent, bufferWriter, cancellationToken).ConfigureAwait(false);

            bufferWriter.Clear();
            writer.Reset();
        }
    }

    private static async ValueTask SendAsync<T>(Stream stream, SseEvent<T> sseEvent, ArrayBufferWriter<byte> bufferWriter, CancellationToken cancellationToken = default)
    {
        if (sseEvent.EventType is { } eventType)
        {
            await stream.WriteAsync(_sseEventFieldPrefix, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(eventType), cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);
        }


        await stream.WriteAsync(_sseDataFieldPrefix, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);


        if (sseEvent.Id is { } id)
        {
            await stream.WriteAsync(_sseIdFieldPrefix, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(id), cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);
        }
        await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);
    }

}


