namespace Showcase.ServiceDefaults.Clients.VectorStore;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class VectorStoreEntityAttribute : Attribute
{
    public string CollectionName { get; init; }
    public string DocumentType { get; init; }
}