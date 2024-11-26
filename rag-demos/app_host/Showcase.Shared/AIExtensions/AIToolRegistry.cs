using Microsoft.Extensions.AI;
using System.Collections;
using System.Diagnostics.CodeAnalysis;


namespace Showcase.Shared.AIExtensions;
public class AIToolRegistry : IAIToolRegistry
{

    private readonly Dictionary<string, AIFunction> _tools;

    public AIToolRegistry() => _tools = new(StringComparer.OrdinalIgnoreCase);

    public AIToolRegistry(IEnumerable<AIFunction> tools)
    {
        if (tools is AIToolRegistry existing)
        {
            _tools = new(existing._tools, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _tools = new(tools is ICollection<AIFunction> c ? c.Count : 0, StringComparer.OrdinalIgnoreCase);
            AddRange(tools);
        }
    }

    void ICollection<AIFunction>.CopyTo(AIFunction[] array, int arrayIndex) =>
        ((IDictionary<string, AIFunction>)_tools).Values.CopyTo(array, arrayIndex);

    bool ICollection<AIFunction>.IsReadOnly => false;

    public int Count => _tools.Count;


    public AIFunction this[string name]
    {
        get
        {
            if (!TryGetTool(name, out AIFunction? tool))
            {
                throw new KeyNotFoundException($"AI Tool {name} not found.");
            }

            return tool;
        }
    }

    public void Add(AIFunction item)
    {
        string name = item.Metadata.Name;

        _tools.Add(name, item);
    }

    public void AddRange(IEnumerable<AIFunction> tools)
    {

        foreach (AIFunction tool in tools)
        {
            Add(tool);
        }
    }

    public void Clear() => _tools.Clear();

    public bool Contains(AIFunction item) => _tools.ContainsValue(item);

    public void CopyTo(AIFunction[] array, int arrayIndex) =>
        ((IDictionary<string, AIFunction>)_tools).Values.CopyTo(array, arrayIndex);

    public bool Remove(AIFunction item) => _tools.Remove(item.Metadata.Name);

    public IEnumerator<AIFunction> GetEnumerator() => _tools.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetTool(string name, [NotNullWhen(true)] out AIFunction? plugin) => _tools.TryGetValue(name, out plugin);
}
