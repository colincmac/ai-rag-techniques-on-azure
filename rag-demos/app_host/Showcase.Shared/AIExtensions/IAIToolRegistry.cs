using Microsoft.Extensions.AI;
using System.Diagnostics.CodeAnalysis;

namespace Showcase.Shared.AIExtensions;
public interface IAIToolRegistry : ICollection<AIFunction>, IReadOnlyCollection<AIFunction>
{
    /// <summary>Gets a plugin from the collection by name.</summary>
    /// <param name="name">The name of the plugin.</param>
    /// <returns>The plugin.</returns>
    AIFunction this[string name] { get; }

    /// <summary>Gets a plugin from the collection by name.</summary>
    /// <param name="name">The name of the plugin.</param>
    /// <param name="plugin">The plugin if found in the collection.</param>
    /// <returns>true if the collection contains the plugin; otherwise, false.</returns>
    bool TryGetTool(string name, [NotNullWhen(true)] out AIFunction? plugin);

    /// <summary>Adds a range of plugins to the collection.</summary>
    void AddRange(IEnumerable<AIFunction> tools);
}
