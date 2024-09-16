public class IncidentIndexRecord
{
    public static SearchIndex GetIndex(Configuration configuration)
    {
        const string vectorSearchHnswProfile = "my-vector-profile";
        const string vectorSearchExhasutiveKnnProfile = "myExhaustiveKnnProfile";
        const string vectorSearchHnswConfig = "myHnsw";
        const string vectorSearchExhaustiveKnnConfig = "myExhaustiveKnn";
        const string vectorSearchVectorizer = "myOpenAIVectorizer";
        const string semanticSearchConfig = "my-semantic-config";
        const int modelDimensions = 1536;

        SearchIndex searchIndex = new(configuration.IndexName)
        {
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig)
                    {
                        Vectorizer = vectorSearchVectorizer
                    },
                    new VectorSearchProfile(vectorSearchExhasutiveKnnProfile, vectorSearchExhaustiveKnnConfig)
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchHnswConfig),
                    new ExhaustiveKnnAlgorithmConfiguration(vectorSearchExhaustiveKnnConfig)
                },
                Vectorizers =
                {
                    new AzureOpenAIVectorizer(vectorSearchVectorizer)
                    {
                        AzureOpenAIParameters = new AzureOpenAIParameters()
                        {
                            ResourceUri = new Uri(configuration.AzureOpenAIEndpoint),
                            ApiKey = configuration.AzureOpenAIApiKey,
                            DeploymentId = configuration.AzureOpenAIEmbeddingDeployedModel,
                        }
                    }
                }
            },
            SemanticSearch = new()
            {
                Configurations =
                {
                    new SemanticConfiguration(semanticSearchConfig, new()
                    {
                        TitleField = new SemanticField(fieldName: "title"),
                        ContentFields =
                        {
                            new SemanticField(fieldName: "chunk")
                        },
                    })
                },
            },
            Fields =
            {
                new SearchableField("parent_id") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField("chunk_id") { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true, AnalyzerName = LexicalAnalyzerName.Keyword },
                new SearchableField("title"),
                new SearchableField("chunk"),
                new SearchField("vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = modelDimensions,
                    VectorSearchProfileName = vectorSearchHnswProfile
                },
                new SearchableField("category") { IsFilterable = true, IsSortable = true, IsFacetable = true },
            },
        };

        return searchIndex;
    }
}