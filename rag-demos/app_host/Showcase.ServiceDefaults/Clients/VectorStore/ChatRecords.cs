#pragma warning disable SKEXP0001

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using System.Text.Json.Serialization;

namespace Showcase.ServiceDefaults.Clients.VectorStore;

[VectorStoreEntity(CollectionName = "chat", DocumentType = "ChatThreadMessage")]
public record ChatThreadMessage(
    [property: VectorStoreRecordPartitionKey] string UserId,
    [property: VectorStoreRecordPartitionKey] string ThreadId,
    [property: VectorStoreRecordKey, VectorStoreRecordPartitionKey] string Id
)
{
    public string Type { get; set; } = "ChatThreadMessage";
    public int Tokens { get; set; } = 0;
    public AuthorRole Role { get; set; } = AuthorRole.User;
    public string AuthorName { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string Content { get; set; } = string.Empty;

    public bool Deleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public bool CacheHit { get; set; } = false;

    [JsonIgnore]
    public string? CacheReferenceId { get; set; }

    [VectorStoreRecordVector]
    public ReadOnlyMemory<float> Vector { get; set; } = default;

    public static implicit operator ChatMessageContent(ChatThreadMessage v) => new ChatMessageContent(v.Role, v.Content)
    {
        AuthorName = v.AuthorName
    };
};

[VectorStoreEntity(CollectionName = "chat", DocumentType = "chatThread")]
public record ChatThread(
    [property: VectorStoreRecordPartitionKey] string UserId,
    [property: VectorStoreRecordPartitionKey] string ThreadId,
    string DisplayName = "New Chat"
)
{
    public string Type { get; set; } = "ChatThread";

    [VectorStoreRecordKey]
    public string Id { get; set; } = ThreadId;

    public bool Deleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public IEnumerable<ChatThreadMessage> Messages { get; set; } = new List<ChatThreadMessage>();

    public string GetContextWindowWithinLimit(int maxTokens, int currentTokens = 0) => Messages.Reverse().TakeWhile((x, i) =>
    {
        currentTokens += x.Tokens;
        return currentTokens <= maxTokens;
    }).Select(m => m.Content).Aggregate(new StringBuilder(), (sb, value) => sb.AppendLine(value), sb => sb.ToString());

    [JsonIgnore]
    public string FullContextWindow => Messages.Select(m => m.Content).Aggregate(new StringBuilder(), (sb, value) => sb.AppendLine(value), sb => sb.ToString());

    public IEnumerable<string> GetMessageContentsForRole(AuthorRole role) => Messages.Where(x => x.Role == role).Select(x => x.Content);
    public IEnumerable<string> GetAllMessageContents() => Messages.Select(x => x.Content);
    public static implicit operator ChatHistory(ChatThread v) => new ChatHistory(messages: v.Messages.Select(m => (ChatMessageContent)m));
};
