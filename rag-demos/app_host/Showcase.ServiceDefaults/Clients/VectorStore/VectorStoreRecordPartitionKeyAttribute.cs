namespace Showcase.ServiceDefaults.Clients.VectorStore;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class VectorStoreRecordPartitionKeyAttribute : Attribute
{
    public int? HierarchyLevel { get; set; }
}
