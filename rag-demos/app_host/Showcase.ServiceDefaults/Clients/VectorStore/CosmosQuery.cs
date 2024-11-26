using Microsoft.Azure.Cosmos;

namespace Showcase.ServiceDefaults.Clients.VectorStore;
public record CosmosQuery(PartitionKey PartitionKey)
{
    public string Id { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
};
