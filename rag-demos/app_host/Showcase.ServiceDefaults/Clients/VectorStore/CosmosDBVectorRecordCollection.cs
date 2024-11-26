#pragma warning disable SKEXP0001
using CommunityToolkit.Diagnostics;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Embeddings;
using System.Runtime.CompilerServices;
using System.Text;


namespace Showcase.ServiceDefaults.Clients.VectorStore;
public class CosmosDBVectorRecordCollection<TRecord> : IVectorStoreRecordCollection<CosmosQuery, TRecord>
{
    public readonly VectorStoreEntityDefinition entityDefinition;

    public string CollectionName => entityDefinition.CollectionName;
    private static readonly VectorSearchOptions s_defaultVectorSearchOptions = new();

    private readonly Container _container;
    private readonly Tokenizer _tokenizer;
    private readonly ITextEmbeddingGenerationService _textEmbeddingService;

    private const string SelectClauseDelimiter = ",";
    private const string AndConditionDelimiter = " AND ";
    private const string OrConditionDelimiter = " OR ";

    public CosmosDBVectorRecordCollection(Container? container, Tokenizer tokenizer, ITextEmbeddingGenerationService textEmbeddingService)
    {
        _container = container ?? throw new ArgumentNullException("container", "CosmosDB container must not be null");
        _tokenizer = tokenizer;
        _textEmbeddingService = textEmbeddingService;
        entityDefinition = VectorStoreEntityJsonSchemaParser.GetVectorRecordSchema<TRecord>();
    }

    private QueryDefinition GetCosmosQueryDefinition(CosmosQuery query)
    {
        const string RecordKeyVariableName = "@rk";
        const string DocumentTypeVariableName = "@dt";

        const string TableAlias = "c";

        var parameters = new List<(string Variable, string Value)>();

        string queryText = $"SELECT * FROM {TableAlias}";

        // Construct WHERE clause
        var optionalWhereStatement = string.Empty;

        if (!string.IsNullOrEmpty(query.Id))
        {
            parameters.Add((RecordKeyVariableName, query.Id));
            optionalWhereStatement += $"{TableAlias}.{entityDefinition.IdField} = {RecordKeyVariableName} ";
        }

        if (!string.IsNullOrEmpty(query.DocumentType))
        {
            parameters.Add((DocumentTypeVariableName, query.DocumentType));
            optionalWhereStatement += $"{AndConditionDelimiter} {TableAlias}.{entityDefinition.DocumentType} = {DocumentTypeVariableName}";
        }

        if (!string.IsNullOrEmpty(optionalWhereStatement))
        {
            queryText += $" WHERE {optionalWhereStatement}";
        }

        var queryDefinition = new QueryDefinition(queryText);

        foreach (var parameter in parameters)
        {
            queryDefinition.WithParameter(parameter.Variable, parameter.Value);
        }
        return queryDefinition;
    }

    public async Task<TRecord?> GetAsync(CosmosQuery query, GetRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await _container.ReadItemAsync<TRecord>(query.Id, query.PartitionKey, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<TRecord> GetBatchAsync(IEnumerable<CosmosQuery> keys, GetRecordOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queryDefinitions = keys.Select(k => (PartitionKey: k.PartitionKey, QueryDefinition: GetCosmosQueryDefinition(k)));
        foreach (var query in queryDefinitions)
        {
            await foreach (var record in GetItemsAsync(query.PartitionKey, query.QueryDefinition, cancellationToken))
            {
                yield return record;
            }
        }
    }

    public Task<VectorSearchResults<TRecord>> VectorizedSearchAsync<TVector>(TVector vector, VectorSearchOptions? options = null, CancellationToken cancellationToken = default)
    {
        const string VectorVariableName = "@vector";
        const string OffsetVariableName = "@offset";
        const string LimitVariableName = "@limit";
        const string TopVariableName = "@top";
        const string TableAlias = "c";
        const string ScorePropertyName = "SimilarityScore";

        Guard.IsNotNull(vector);

        var fieldsArgument = entityDefinition.AllFields.Select(field => $"{TableAlias}.{field}");
        var vectorDistanceArgument = $"VectorDistance({TableAlias}.{entityDefinition.VectorField}, {VectorVariableName})";
        var vectorDistanceArgumentWithAlias = $"{vectorDistanceArgument} AS {ScorePropertyName}";

        var selectClauseArguments = string.Join(SelectClauseDelimiter, [.. fieldsArgument, vectorDistanceArgumentWithAlias]);

        VectorSearchOptions searchOptions = options ?? s_defaultVectorSearchOptions;

        var filterClauses = searchOptions?.Filter?.FilterClauses.ToList();
        var filter = BuildSearchFilter(searchOptions?.Filter);
        var filterQueryParameters = filter?.QueryParameters;
        var filterWhereClauseArguments = filter?.WhereClauseArguments;

        var queryParameters = new Dictionary<string, object>
        {
            [VectorVariableName] = vector
        };

        var whereClause = filterWhereClauseArguments is { Count: > 0 } ?
            $"WHERE {string.Join(AndConditionDelimiter, filterWhereClauseArguments)}" :
            string.Empty;

        var topArgument = searchOptions.Skip == 0 ? $"TOP {TopVariableName} " : string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine($"SELECT {topArgument}{selectClauseArguments}");
        builder.AppendLine($"FROM {TableAlias}");

        if (filterWhereClauseArguments is { Count: > 0 })
        {
            builder.AppendLine($"WHERE {string.Join(AndConditionDelimiter, filterWhereClauseArguments)}");
        }

        builder.AppendLine($"ORDER BY {vectorDistanceArgument}");

        if (!string.IsNullOrEmpty(topArgument))
        {
            queryParameters.Add(TopVariableName, searchOptions.Top);
        }
        else
        {
            builder.AppendLine($"OFFSET {OffsetVariableName} LIMIT {LimitVariableName}");
            queryParameters.Add(OffsetVariableName, options.Skip);
            queryParameters.Add(LimitVariableName, options.Top);
        }
        throw new NotImplementedException();
    }

    public Task<CosmosQuery> UpsertAsync(TRecord record, UpsertRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        //await _container.UpsertItemAsync(record, cancellationToken: cancellationToken);
        throw new NotImplementedException();

    }

    public IAsyncEnumerable<CosmosQuery> UpsertBatchAsync(IEnumerable<TRecord> records, UpsertRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        //foreach (var record in records)
        //{
        //    yield await UpsertAsync(record, options, cancellationToken: cancellationToken);
        //}
        throw new NotImplementedException();

    }


    public Task DeleteAsync(CosmosQuery key, DeleteRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteBatchAsync(IEnumerable<CosmosQuery> keys, DeleteRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async IAsyncEnumerable<TRecord> GetItemsAsync(PartitionKey partitionKey, QueryDefinition queryDefinition, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var iterator = _container
            .GetItemQueryIterator<TRecord>(queryDefinition, requestOptions: new()
            {
                PartitionKey = partitionKey
            });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var record in response.Resource)
            {
                if (record is not null)
                {
                    yield return record;
                }
            }
        }
    }

    private static AzureCosmosDBNoSQLFilter? BuildSearchFilter(
        VectorSearchFilter? filter)
    {
        const string EqualOperator = "=";
        const string ArrayContainsOperator = "ARRAY_CONTAINS";
        const string ConditionValueVariableName = "@cv";
        const string TableAlias = "c";

        var filterClauses = filter?.FilterClauses.ToList();

        if (filterClauses is not { Count: > 0 })
        {
            return null;
        }

        var whereClauseArguments = new List<string>();
        var queryParameters = new Dictionary<string, object>();

        for (var i = 0; i < filterClauses.Count; i++)
        {
            var filterClause = filterClauses[i];

            string queryParameterName = $"{ConditionValueVariableName}{i}";
            object queryParameterValue;
            string whereClauseArgument;

            if (filterClause is EqualToFilterClause equalToFilterClause)
            {
                whereClauseArgument = $"{TableAlias}.{equalToFilterClause.FieldName} {EqualOperator} {queryParameterName}";
                queryParameterValue = equalToFilterClause.Value;
            }
            else if (filterClause is AnyTagEqualToFilterClause anyTagEqualToFilterClause)
            {
                whereClauseArgument = $"{ArrayContainsOperator}({TableAlias}.{anyTagEqualToFilterClause.FieldName}, {queryParameterName})";
                queryParameterValue = anyTagEqualToFilterClause.Value;
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported filter clause type '{filterClause.GetType().Name}'. " +
                    $"Supported filter clause types are: {string.Join(", ", [
                        nameof(EqualToFilterClause),
                        nameof(AnyTagEqualToFilterClause)])}");
            }

            whereClauseArguments.Add(whereClauseArgument);
            queryParameters.Add(queryParameterName, queryParameterValue);
        }

        return new AzureCosmosDBNoSQLFilter(whereClauseArguments, queryParameters);
    }

    internal record AzureCosmosDBNoSQLFilter(
        List<string> WhereClauseArguments,
        Dictionary<string, object> QueryParameters
    );

    #region Not implemented
    public Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    public Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CreateCollectionAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CreateCollectionIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    #endregion
}
