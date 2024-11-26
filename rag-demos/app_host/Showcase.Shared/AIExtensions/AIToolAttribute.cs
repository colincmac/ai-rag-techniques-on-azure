namespace Showcase.Shared.AIExtensions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AIToolAttribute : Attribute
{
    public AIToolAttribute(string? name = default, string? description = default)
    {
        Name = name;
        Description = description;
    }

    public string? Name { get; }

    public string? Description { get; }
}
