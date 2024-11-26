#pragma warning disable SKEXP0001

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Embeddings;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Showcase.ServiceDefaults.Clients.VectorStore;
public class CosmosDBVectorStore : IVectorStore
{
    private readonly Database _database;
    private readonly Tokenizer _tokenizer;
    private readonly ITextEmbeddingGenerationService _textEmbeddingService;

    public CosmosDBVectorStore(Database database, Tokenizer tokenizer, ITextEmbeddingGenerationService textEmbeddingService)
    {
        _database = database;
        _tokenizer = tokenizer;
        _textEmbeddingService = textEmbeddingService;
    }

    public IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition = null)
        where TKey : notnull
    {
        var container = _database.GetContainer(name);
        var recordCollection = new CosmosDBVectorRecordCollection<TRecord>(container, _tokenizer, _textEmbeddingService) as IVectorStoreRecordCollection<TKey, TRecord>;
        return recordCollection!;
    }

    public IVectorStoreRecordCollection<CosmosQuery, TRecord> GetContainer<TRecord>()
    {
        var containerName = typeof(TRecord).GetCustomAttribute<VectorStoreEntityAttribute>().CollectionName;
        var container = _database.GetContainer(containerName);
        return new CosmosDBVectorRecordCollection<TRecord>(container, _tokenizer, _textEmbeddingService);
    }

    public async IAsyncEnumerable<string> ListCollectionNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string Query = "SELECT VALUE(c.id) FROM c";

        using var feedIterator = _database.GetContainerQueryIterator<string>(Query);

        while (feedIterator.HasMoreResults)
        {
            var next = await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var containerName in next.Resource)
            {
                yield return containerName;
            }
        }
    }
}
