using Microsoft.Extensions.AI;

namespace Showcase.Shared.AIExtensions;
public interface IAIToolHandler
{
    IEnumerable<AIFunction> GetAITools();
}
