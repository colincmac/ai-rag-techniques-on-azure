using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.Shared.AIExtensions.Realtime.AudioDataProvider;
public interface IAudioDataProvider
{
    Task<ReadOnlyMemory<byte>?> ReadNextChunkAsync(CancellationToken cancellationToken);
}
