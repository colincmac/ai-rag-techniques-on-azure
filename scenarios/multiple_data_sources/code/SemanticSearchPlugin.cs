public class AzureSearchDocumentsPlugin
{
    // At the time of writing, the Semantic Kernels' embedding service is experimental. However, you can use various methods to generate embeddings
    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;
    private readonly SearchIndexClient _indexClient;

    public AzureSearchDocumentsPlugin(ITextEmbeddingGenerationService textEmbeddingGenerationService, SearchIndexClient indexClient)
    {
        _textEmbeddingGenerationService = textEmbeddingGenerationService;
        _indexClient = indexClient;
    }

    [KernelFunction("Search")]
    [Description("Search for a document similar to the given query.")]
    public async Task<string> SearchAsync(string query)
    {
        // Convert string query to vector
        ReadOnlyMemory<float> embedding = await _textEmbeddingGenerationService.GenerateEmbeddingAsync(query);

        // Get client for search operations
        SearchClient searchClient = _indexClient.GetSearchClient("default-collection");

        // Configure request parameters
        VectorizedQuery vectorQuery = new(embedding);
        vectorQuery.Fields.Add("vector");

        SearchOptions searchOptions = new() { VectorSearch = new() { Queries = { vectorQuery } } };

        // Perform search request
        Response<SearchResults<IndexSchema>> response = await searchClient.SearchAsync<IndexSchema>(searchOptions);

        // Collect search results
        await foreach (SearchResult<IndexSchema> result in response.Value.GetResultsAsync())
        {
            return result.Document.Chunk; // Return text from first result
        }

        return string.Empty;
    }

    private sealed class IndexSchema
    {
        [JsonPropertyName("chunk")]
        public string Chunk { get; set; }

        [JsonPropertyName("vector")]
        public ReadOnlyMemory<float> Vector { get; set; }
    }
}