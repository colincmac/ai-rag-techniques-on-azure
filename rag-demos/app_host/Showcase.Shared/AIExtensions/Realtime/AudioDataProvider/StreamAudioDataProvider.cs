using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.Shared.AIExtensions.Realtime.AudioDataProvider;
public class StreamAudioDataProvider : IAudioDataProvider
{
    private readonly Stream _stream;
    private readonly int _bufferSize;
    private readonly byte[] _buffer;

    public StreamAudioDataProvider(Stream stream, int bufferSize = 16 * 1024)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _bufferSize = bufferSize;
        _buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
    }

    public async Task<ReadOnlyMemory<byte>?> ReadNextChunkAsync(CancellationToken cancellationToken)
    {
        int bytesRead = await _stream.ReadAsync(_buffer, 0, _bufferSize, cancellationToken).ConfigureAwait(false);
        if (bytesRead == 0)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            return null;
        }
        return new ReadOnlyMemory<byte>(_buffer, 0, bytesRead);
    }
}
