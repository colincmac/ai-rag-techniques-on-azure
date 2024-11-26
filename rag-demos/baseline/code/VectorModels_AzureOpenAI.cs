#pragma warning disable SKEXP0001,SKEXP0020

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class VectorStoreEntityAttribute: Attribute
{
    public string CollectionName { get; init; }
    public string DocumentType { get; init; }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class VectorStoreIdAttribute(): Attribute;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class VectorStorePartitionKeyAttribute(): Attribute;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class VectorStoreDocumentTypeAttribute(): Attribute;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class VectorStoreEmbeddingAttribute(): Attribute;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class VectorStoreEmbeddingDataAttribute(): Attribute;

public abstract record VectorStoreEntity
{   
    public int Tokens { get; set; } = 0;

    [JsonIgnore]
    public double SimilarityScore { get; set; } = 0.0;

    [JsonIgnore]
    public double RelevanceScore => (SimilarityScore + 1) / 2;

    public void UpdateTokenCount(Tokenizer tokenizer)
    {
        Tokens = this.GetTokenCount(tokenizer);
    }

    public async Task UpdateEmbedding(ITextEmbeddingGenerationService textEmbeddingService, CancellationToken cancellationToken = default)
    {
        var embedding = await this.GetEmbedding(textEmbeddingService, cancellationToken);
        this.GetType().GetProperty("Embedding")?.SetValue(this, embedding);
    }

    public string GetContextWindow() => this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<VectorStoreEmbeddingAttribute>() != null)
            .Select(p => {
                var value = p.GetValue(this);
                return value switch
                {
                    string stringValue => stringValue,
                    IEnumerable<string> stringList => stringList.Aggregate(new StringBuilder(), (sb, value) => sb.AppendLine(value), sb => sb.ToString()),
                    _ => string.Empty
                };
            })
            .Aggregate(new StringBuilder(), (sb, value) => sb.AppendLine(value), sb => sb.ToString());

    public Task<ReadOnlyMemory<float>> GetEmbedding(ITextEmbeddingGenerationService textEmbeddingService, CancellationToken cancellationToken = default)
    {
        var embeddingString = this.GetContextWindow();
        return textEmbeddingService.GenerateEmbeddingAsync(value: embeddingString, cancellationToken: cancellationToken);
    }

    public int GetTokenCount(Tokenizer s_tokenizer) => s_tokenizer.CountTokens(this.GetContextWindow());

    public PartitionKey GetPartitionKey() => this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<VectorStorePartitionKeyAttribute>() != null)
        .Select(p => p.GetValue(this) as string ?? string.Empty)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Aggregate(new PartitionKeyBuilder(), (pk, value) => pk.Add(value), pk => pk.Build());  

    public string GetId() => this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<VectorStoreIdAttribute>() != null)
        .Select(p => p.GetValue(this) as string)
        .FirstOrDefault();

};

/**
    * Container: Cache
*/
[VectorStoreEntity(CollectionName = "cache")]
public record CacheItem(
    [property: VectorStoreId, VectorStorePartitionKey] string Id, 
    [property: VectorStoreEmbeddingData] string Prompts,
    ChatMessageContent Completion, 
    [property: VectorStoreEmbedding] ReadOnlyMemory<float> Embedding): VectorStoreEntity
{
    public int Ttl => CalculateTtl();
    public int CacheHits { get; set; } = 1; // Start with 1 to avoid immediate eviction
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string AgentName { get; set; } = string.Empty;
    public void RegisterHit()
    {
        CacheHits++;
    }

    private int CalculateTtl()
    {
        var elapsedTime = DateTime.UtcNow - CreatedAt;
        var baseTtl = (int)TimeSpan.FromHours(1).TotalSeconds;
        return CacheHits * baseTtl - (int)elapsedTime.TotalSeconds;
    }
}

// Container Properties
public static ContainerProperties getSemanticCacheContainerProperties() {
    var properties = new ContainerProperties(id: "cache", partitionKeyPath: "/id"){
            IndexingPolicy = new (){
                VectorIndexes = new ()
                {
                    new VectorIndexPath(){
                        Path = "/embedding",
                        Type = VectorIndexType.DiskANN
                    }
                },
            },
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(new (){
                new Embedding(){
                    Path = "/embedding",
                    Dimensions = 1536,
                    DataType = VectorDataType.Float32,
                    DistanceFunction = Microsoft.Azure.Cosmos.DistanceFunction.Cosine
                }
            }),
            DefaultTimeToLive = (int)TimeSpan.FromDays(1).TotalSeconds
    };
    properties.IndexingPolicy.IncludedPaths.Add(new () { Path = "/*" });

    return properties;
} 

/**
    * Container: ChatThreads
*/

// Using Semantic Kernel's ChatHistory class as a building block for ChatThreads.
[VectorStoreEntity(CollectionName = "chat", DocumentType = "ChatThreadMessage")]
public record ChatThreadMessage(
    [property: VectorStoreId] string Id, 
    [property: VectorStorePartitionKey] string UserId, 
    [property: VectorStorePartitionKey] string ThreadId, 
    ChatMessageContent MessageContent
): VectorStoreEntity
{
    public string Type { get; set; } = "ChatThreadMessage";
    public bool Deleted { get; set; } = false;


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public bool CacheHit { get; set; } = false;

    [JsonIgnore]
    public string CacheReferenceId { get; set; } = default;

    [JsonIgnore]
    public bool FinishedStream { get; set; } = true;

    [VectorStoreEmbeddingData,JsonIgnore]
    public string ContextWindow => MessageContent.Content;
    public static implicit operator ChatMessageContent(ChatThreadMessage v) => v.MessageContent;
};

[VectorStoreEntity(CollectionName = "chat", DocumentType = "ChatThread")]
public record ChatThread(
    [property: VectorStorePartitionKey] string UserId, 
    [property: VectorStorePartitionKey] string ThreadId, 
    string DisplayName = "New Chat"
): VectorStoreEntity
{
    public string Type { get; set; } = "ChatThread";
    
    [VectorStoreId]
    public string Id { get; set; } = ThreadId;

    public bool Deleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public IEnumerable<ChatThreadMessage> Messages { get; set; } = new List<ChatThreadMessage>();

    public string GetContextWindowWithinLimit(int maxTokens, int currentTokens = 0) => Messages.Reverse().TakeWhile((x, i) => {
        currentTokens += x.Tokens;
        return currentTokens <= maxTokens;
    }).Select(m => m.ContextWindow).Aggregate(new StringBuilder(), (sb, value) => sb.AppendLine(value), sb => sb.ToString());

    [VectorStoreEmbeddingData,JsonIgnore]
    public string FullContextWindow => Messages.Select(m => m.ContextWindow).Aggregate(new StringBuilder(), (sb, value) => sb.AppendLine(value), sb => sb.ToString());

    public IEnumerable<string> GetMessageContentsForRole(AuthorRole role) => Messages.Where(x => x.MessageContent.Role == role).Select(x => x.MessageContent.Content);
    public IEnumerable<string> GetAllMessageContents() => Messages.Select(x => x.MessageContent.Content);
    public static implicit operator ChatHistory(ChatThread v) => new ChatHistory(messages: v.Messages.Select(m => (ChatMessageContent)m));
};


// Container Properties
public static ContainerProperties getChatThreadContainerProperties() {
    var properties = new ContainerProperties(id: "chat", partitionKeyPaths: new Collection<string>(){ "/userId", "/threadId" });
    properties.IndexingPolicy.ExcludedPaths.Add(new () { Path = "/*" });

    foreach (var path in new string[] { "/userId/?", "/threadId/?"})
    {
        properties.IndexingPolicy.IncludedPaths.Add(new () { Path = path });
    }

    return properties;
}


/**
    * Container: CompanyInfo
*/

public record CompanyOfficer(
    string FirstName,
    string LastName,
    int? Age,
    string Title,
    int? YearBorn,
    long TotalPay
);

public record SecurityListing(
    string Cusip, 
    string Name,  
    string Exchange, 
    string Symbol, 
    string IsinNumber);

[VectorStoreEntity(CollectionName = "companyInfo", DocumentType = "CompanyInfo")]
public record CompanyInfo(
    int Cik,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Sector,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Industry,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string SubIndustry,
    [property: VectorStoreEmbeddingData] string Cusip6,
    string Lei,
    string CompanyName,
    string Address1,
    string City,
    string State,
    string Zip,
    string Country,
    string Phone,
    string Website,
    [property: VectorStoreEmbeddingData] string LongBusinessSummary,
    [property: VectorStoreEmbeddingData] List<string> ReferenceNames,
    [property: VectorStoreEmbeddingData] List<string> Tickers,
    List<CompanyOfficer> CompanyOfficers,
    List<SecurityListing> SecurityListings,
    string WebsiteUrl,
    [property: VectorStoreEmbedding] ReadOnlyMemory<float>? Embedding = null
): VectorStoreEntity
{
    public string Type { get; set; } = "CompanyInfo";

    [VectorStoreId]
    public string Id { get; set; } = Cik.ToString();

    [VectorStorePartitionKey]
    public string PartitionKey { get; set; } = Cik.ToString();
};

[VectorStoreEntity(CollectionName = "companyInfo", DocumentType = "10-K-section")]
public record Form10KSection(
    int Cik,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Sector,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Industry,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string SubIndustry,
    string SequenceId, 
    DateTime FilingDate, 
    string SectionName, 
    string SectionShortName, 
    [property: VectorStoreEmbeddingData] string SectionText, 
    [property: VectorStoreEmbedding] ReadOnlyMemory<float>? Embedding = null, 
    string Type = "10-K-section"): VectorStoreEntity
    {

    [VectorStoreId]
    public string Id { get; set; } = $"{Cik}_{FilingDate:yyyy-MM-dd}_{Type}_{SectionName}";

    [VectorStorePartitionKey]
    public string PartitionKey { get; set; } = Cik.ToString();

    [Description("The CIK of the manager who filed the SEC document.")]
    public string FilerCik { get; set; } = Cik.ToString();

    [JsonIgnore, Description("The Accession Number is a unique identifier assigned by the SEC to each filing.")]
    public string AccessionNumber => $"{FilerCik.ToString().PadLeft(10, '0')}-{FilingDate:yy}-{SequenceId.PadLeft(4, '0')}";

    [JsonIgnore]
    public Uri SourceUri => new Uri($"https://www.sec.gov/Archives/edgar/data/{Cik}/{AccessionNumber}.txt");
}

[VectorStoreEntity(CollectionName = "companyInfo", DocumentType = "13D")]
public record Form13D(
    int Cik, 
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Sector,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Industry,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string SubIndustry,
    string SequenceId, 
    string ReportingPerson, 
    DateTime FilingDate, 
    string Description, 
    ReadOnlyMemory<float>? Embedding = null, 
    string Type = "13D"): VectorStoreEntity
{

    [VectorStoreId]
    public string Id { get; set; } = $"{Cik}_{FilingDate:yyyy-MM-dd}_{Type}";

    [VectorStorePartitionKey]
    public string PartitionKey { get; set; } = Cik.ToString();

    [Description("The CIK of the manager who filed the SEC document.")]
    public string FilerCik { get; set; } = Cik.ToString();

    [JsonIgnore, Description("The Accession Number is a unique identifier assigned by the SEC to each filing.")]
    public string AccessionNumber => $"{FilerCik.ToString().PadLeft(10, '0')}-{FilingDate:yy}-{SequenceId.PadLeft(4, '0')}";

    [JsonIgnore]
    public Uri SourceUri => new Uri($"https://www.sec.gov/Archives/edgar/data/{Cik}/{AccessionNumber}.txt");
}

[VectorStoreEntity(CollectionName = "companyInfo", DocumentType = "13F-HR")]
public record Form13FHR(
    int Cik, 
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Sector,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string Industry,
    [property: VectorStoreEmbeddingData, VectorStorePartitionKey] string SubIndustry,
    string SequenceId, 
    string Cusip,  
    DateTime FilingDate, 
    int ManagerCik, 
    string ManagerName, 
    string SecurityName, 
    int Shares, 
    int Value, 
    string SecurityType, 
    ReadOnlyMemory<float>? Embedding = null, 
    string Type = "13F-HR"): VectorStoreEntity
{
    [VectorStoreId]
    public string Id { get; set; } = $"{Cik}_{FilingDate:yyyy-MM-dd}_{Type}";

    [VectorStorePartitionKey]
    public string PartitionKey { get; set; } = Cik.ToString();

    [Description("The CIK of the manager who filed the SEC document.")]
    public string FilerCik { get; set; } = Cik.ToString();

    [JsonIgnore, Description("The Accession Number is a unique identifier assigned by the SEC to each filing.")]
    public string AccessionNumber => $"{FilerCik.ToString().PadLeft(10, '0')}-{FilingDate:yy}-{SequenceId.PadLeft(4, '0')}";

    [JsonIgnore]
    public Uri SourceUri => new Uri($"https://www.sec.gov/Archives/edgar/data/{Cik}/{AccessionNumber}.txt");
}

// Container Properties
public static ContainerProperties getCompanyInfoContainerProperties() {
    var properties = new ContainerProperties(id: "companyInfo", partitionKeyPath: "/partitionKey"){
            IndexingPolicy = new (){
                VectorIndexes = new ()
                {
                    new VectorIndexPath(){
                        Path = "/embedding",
                        Type = VectorIndexType.DiskANN
                    }
                },
            },
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(new (){
                new Embedding(){
                    Path = "/embedding",
                    Dimensions = 1536,
                    DataType = VectorDataType.Float32,
                    DistanceFunction = Microsoft.Azure.Cosmos.DistanceFunction.Cosine
                }
            })
    };
    properties.IndexingPolicy.IncludedPaths.Add(new () { Path = "/*" });

    properties.IndexingPolicy.ExcludedPaths.Add(new () { Path = "/embedding/*" });
    properties.IndexingPolicy.ExcludedPaths.Add(new () { Path = "/sectionText/*" });
    properties.IndexingPolicy.ExcludedPaths.Add(new () { Path = "/longBusinessSummary/*" });

    return properties;
} 

/**
    * Container: MarketData
*/
public record DailyStockMarketReport(int Cik, string Symbol, DateTime Date, float Open, float High, float Low, float Close, long Volume) {
    public string Type => "DailyMarketData";
    public string Id { get; set; } = $"{Symbol}_{Date:yyyy-MM-dd}";
    public string PartitionKey { get; set; } = Symbol;
}

public record NewsArticle(string Headline, string ArticleText, string SourceName, string Uri, DateTime PublishDate){
    public string Type => "NewsArticle";
    public string Id => $"{SourceName}_{Uri}";
    public string PartitionKey { get; set; } = SourceName;
}

public static ContainerProperties getMarketDataContainerProperties() {
    var properties = new ContainerProperties(id: "marketData", partitionKeyPath: "/partitionKey"){
            IndexingPolicy = new (){
                VectorIndexes = new ()
                {
                    new VectorIndexPath(){
                        Path = "/embedding",
                        Type = VectorIndexType.DiskANN
                    }
                },
            },
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(new (){
                new Embedding(){
                    Path = "/embedding",
                    Dimensions = 1536,
                    DataType = VectorDataType.Float32,
                    DistanceFunction = Microsoft.Azure.Cosmos.DistanceFunction.Cosine
                }
            })
    };
    properties.IndexingPolicy.ExcludedPaths.Add(new () { Path = "/embedding/*" });
    properties.IndexingPolicy.ExcludedPaths.Add(new () { Path = "/sectionText/*" });
    properties.IndexingPolicy.ExcludedPaths.Add(new () { Path = "/longBusinessSummary/*" });

    return properties;
} 

var Form10KSectionDescriptions = new Dictionary<string, string>
{
    { "item1", "Business: requires a description of the company’s business, including its main products and services, what subsidiaries it owns, and what markets it operates in" },
    { "item1a", "Risk Factors: includes information about the most significant risks that apply to the company or to its securities" },
    { "item1b", "Unresolved Staff Comments: requires the company to explain certain comments it has received from the SEC staff on previously filed reports that have not been resolved after an extended period of time" },
    { "item2", "Properties: includes information about the company’s significant properties, such as principal plants, mines and other materially important physical properties" },
    { "item3", "Legal Proceedings: requires the company to include information about significant pending lawsuits or other legal proceedings, other than ordinary litigation" },
    { "item7", "Management’s Discussion and Analysis of Financial Condition and Results of Operations (MD&A): gives the company’s perspective on the business results of the past financial year. This section, known as the MD&A for short, allows company management to tell its story in its own words" },
    { "item7a", "Quantitative and Qualitative Disclosures About Market Risk: requires information about the company’s exposure to market risk, such as interest rate risk, foreign currency exchange risk, commodity price risk or equity price risk" },
    { "item8", "Financial Statements and Supplementary Data: requires the company’s audited financial statements" },
    { "item10", "Directors, Executive Officers and Corporate Governance: requires information about the background and experience of the company’s directors and executive officers, the company’s code of ethics, and certain qualifications for directors and committees of the board of directors" },
    { "item11", "Executive Compensation: includes detailed disclosure about the company’s compensation policies and programs and how much compensation was paid to the top executive officers of the company in the past year" },
    { "item15", "Exhibits, Financial Statement Schedules: Many exhibits are required, including documents such as the company’s bylaws, copies of its material contracts, and a list of the company’s subsidiaries" }
};

public class VectorStore
{
    private readonly Database _database;
    private readonly Tokenizer _tokenizer;
    private readonly ITextEmbeddingGenerationService _textEmbeddingService;

    public VectorStore(Database database, Tokenizer tokenizer, ITextEmbeddingGenerationService textEmbeddingService)
    {
        _database = database;
        _tokenizer = tokenizer;
        _textEmbeddingService = textEmbeddingService;
    }

    public async Task CreateContainers()
    {
        await _database.CreateContainerIfNotExistsAsync(getSemanticCacheContainerProperties());
        await _database.CreateContainerIfNotExistsAsync(getChatThreadContainerProperties());
        await _database.CreateContainerIfNotExistsAsync(getCompanyInfoContainerProperties());
    }

    public VectorCollection<TRecord> GetContainer<TRecord>() where TRecord : VectorStoreEntity
    {
        var containerName = typeof(TRecord).GetCustomAttribute<VectorStoreEntityAttribute>().CollectionName;
        var container = _database.GetContainer(containerName);
        return new VectorCollection<TRecord>(container, _tokenizer, _textEmbeddingService);
    }

    public async IAsyncEnumerable<string> ListCollectionNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string Query = "SELECT VALUE(c.id) FROM c";

        using var feedIterator = this._database.GetContainerQueryIterator<string>(Query);

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

public class VectorCollection<TRecord> where TRecord : VectorStoreEntity
{
    public readonly string DocumentType = typeof(TRecord).GetCustomAttribute<VectorStoreEntityAttribute>().DocumentType;
    private readonly Container _container;
    private readonly Tokenizer _tokenizer;
    private readonly ITextEmbeddingGenerationService _textEmbeddingService;
    private readonly string _idPropertyName;
    private readonly string[] _partitionKeyPropertyNames;
    private readonly string _embeddingPropertyName;
    
    public VectorCollection(Container container, Tokenizer tokenizer, ITextEmbeddingGenerationService textEmbeddingService)
    {
        _container = container;
        _tokenizer = tokenizer;
        _textEmbeddingService = textEmbeddingService;
        _idPropertyName = GetIdPropertyName();
        _partitionKeyPropertyNames = GetPartitionKeyPropertyNames().ToArray();
        _embeddingPropertyName = GetEmbeddedFieldPropertyName();
    }

    public async Task UpsertAsync(TRecord item, bool updateEmbeddingFields = false, CancellationToken cancellationToken = default)
    {
        if(updateEmbeddingFields)
        {
            item.UpdateTokenCount(_tokenizer);
            await item.UpdateEmbedding(_textEmbeddingService);
        }
        
        await _container.UpsertItemAsync(item, cancellationToken: cancellationToken);
    }

    public async Task UpsertBatch(IEnumerable<TRecord> items, bool updateEmbeddingFields = false, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await UpsertAsync(item, updateEmbeddingFields, cancellationToken);
        }
    }

    public async Task<TRecord> GetAsync(string id, PartitionKey partitionKey, CancellationToken cancellationToken = default)
    {
        return await _container.ReadItemAsync<TRecord>(id, partitionKey, cancellationToken: cancellationToken);
    }

    public async Task<TRecord> GetAsync(TRecord record, CancellationToken cancellationToken = default)
    {
        var id = record.GetId();
        var partitionKey = record.GetPartitionKey();
        return await GetAsync(id, partitionKey, cancellationToken);
    }

    public Task RemoveAsync(TRecord item, CancellationToken cancellationToken = default)
    {
        return _container.DeleteItemAsync<TRecord>(item.GetId(), item.GetPartitionKey(), cancellationToken: cancellationToken);
    }

    /**
    * Find items in the collection that match the predicate and return the selected fields.
    * @param predicate The predicate to filter the items.
    * @param select The fields to return.
    * @param maxResults The maximum number of results to return.
    * @param (Optional) queryEmbedding The embedding to use for similarity search.
    * @param (Optional) minRelevance The minimum relevance score for the results if using a VectorDistance.
    */
    public async IAsyncEnumerable<TRecord> FindItems(Expression<Func<TRecord, bool>> predicate, Expression<Func<TRecord, object>> select, int maxResults, ReadOnlyMemory<float>? queryEmbedding = null, double? minRelevance = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string VectorVariableName = "@vectors";
        const string LimitVariableName = "@limit";
        const string MinRelevanceVariableName = "@minRelevanceScore";
        const string TableAlias = "c";
        const string FromTableAlias = "s";

        var usingEmbedding = queryEmbedding is not null && minRelevance is not null;

        // Where statement from predicate
        var whereStatement = GetCosmosWhere(predicate, TableAlias);   
        
        // Select statements from projection/select
        var extractedSelectStatement = GetCosmosSelect(select, TableAlias);
        var extractedFromSelectStatement = GetCosmosSelect(select, FromTableAlias);

        // If using embedding, add the similarity score to the select statement
        var optionalOrderByStatement = string.Empty;

        if(usingEmbedding)
        {
            whereStatement += $" AND {TableAlias}.similarityScore >= {MinRelevanceVariableName}";
            extractedFromSelectStatement += $", VectorDistance({FromTableAlias}.{_embeddingPropertyName}, {VectorVariableName}, false) as similarityScore";
            optionalOrderByStatement += $"""
            ORDERBY 
                {TableAlias}.similarityScore desc
            """;
        } 

        var queryText = $"""
            SELECT TOP {LimitVariableName}
                {extractedSelectStatement}
            FROM 
                (SELECT {extractedFromSelectStatement} FROM {FromTableAlias})
            {TableAlias}
            WHERE 
                {whereStatement}
            {optionalOrderByStatement}
            """;
        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter(LimitVariableName, maxResults);

        if(usingEmbedding)
        {
            queryDefinition.WithParameter(VectorVariableName, queryEmbedding.Value.ToArray());
            queryDefinition.WithParameter(MinRelevanceVariableName, minRelevance);
        }

        using var feedIterator = this._container
         .GetItemQueryIterator<TRecord>(queryDefinition);

        while (feedIterator.HasMoreResults)
        {
            foreach (var document in await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return document;
            }
        }
    }

    public async IAsyncEnumerable<(TRecord, double)> GetNearestMatchesAsync(
        ReadOnlyMemory<float> embedding,
        string[] fields,
        int limit = 1,
        double minRelevanceScore = 0.0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string VectorVariableName = "@vectors";
        const string LimitVariableName = "@limit";
        const string MinRelevanceVariableName = "@minRelevanceScore";

        Func<string, string> getSelectItems = (string prefix) => string.Join($", {prefix}.", fields);

        string queryText = $"""
            SELECT 
                Top {LimitVariableName} 
                {getSelectItems("p")}
            FROM 
                (SELECT {getSelectItems("s")},
                VectorDistance(s.{_embeddingPropertyName}, {VectorVariableName}, false) as similarityScore FROM s) 
            p 
            WHERE 
                p.similarityScore >= {MinRelevanceVariableName}
            ORDER BY 
                p.similarityScore desc
            """;

        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter(VectorVariableName, embedding.ToArray())
            .WithParameter(LimitVariableName, limit)
            .WithParameter(MinRelevanceVariableName, minRelevanceScore);

        using var feedIterator = this._container
         .GetItemQueryIterator<TRecord>(queryDefinition);

        while (feedIterator.HasMoreResults)
        {
            foreach (var document in await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false))
            {
                var relevanceScore = (document.SimilarityScore + 1) / 2;
                if (relevanceScore >= minRelevanceScore)
                {
                    yield return (document, relevanceScore);
                }
            }
        }
    }

    public async IAsyncEnumerable<ItemResponse<TRecord>> DeleteNearestMatchAsync(
        ReadOnlyMemory<float> embedding,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string VectorVariableName = "@vectors";
        const string SimilarityScoreVariableName = "@similarityScore";
        double similarityScore = 0.99;

        string queryText = $"""
            SELECT 
                Top 1 c.id
            FROM 
                (SELECT c.id,
                VectorDistance(s.{_embeddingPropertyName}, {VectorVariableName}, false) as similarityScore FROM c)
            p 
            WHERE 
                p.similarityScore >= {SimilarityScoreVariableName}
            ORDER BY 
                p.similarityScore desc
            """;

        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter(VectorVariableName, embedding.ToArray())
            .WithParameter(SimilarityScoreVariableName, similarityScore);

        using var feedIterator = this._container
         .GetItemQueryIterator<TRecord>(queryDefinition);

        while (feedIterator.HasMoreResults)
        {
            foreach (var document in await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return await _container.DeleteItemAsync<TRecord>(document.GetId(), document.GetPartitionKey(), cancellationToken: cancellationToken);
            }
        }
    }

    private string GetEmbeddedFieldPropertyName() => typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<VectorStoreEmbeddingAttribute>() != null)
        .Select(p => JsonSerializer.Serialize(p.Name, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
        .FirstOrDefault();

    private IEnumerable<string> GetPartitionKeyPropertyNames() => typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<VectorStorePartitionKeyAttribute>() != null)
        .Select(p => JsonSerializer.Serialize(p.Name, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

    private string GetIdPropertyName() => typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<VectorStoreIdAttribute>() != null)
        .Select(p => JsonSerializer.Serialize(p.Name, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
        .First();

    private string GetCosmosSelect(Expression<Func<TRecord, object>> expression, string tableAlias)
    {
        if (expression.Body is NewExpression newExpression)
        {
            var sb = new StringBuilder();
            foreach (var arg in newExpression.Arguments)
            {
                if (arg is MemberExpression memberExpression)
                {
                    var camelCaseName = Char.ToLowerInvariant(memberExpression.Member.Name[0]) + memberExpression.Member.Name.Substring(1);
                    sb.Append($"{tableAlias}.{camelCaseName}").Append(", ");
                }
            }
            if (sb.Length > 2) sb.Length -= 2; // Remove trailing ", "
            return sb.ToString();
        }
        throw new ArgumentException("Expression is not a valid select statement", nameof(expression));
    }

    private string GetCosmosWhere(Expression<Func<TRecord, bool>> predicate, string tableAlias)
    {
        // Predicate can't contain the embedding field, but we'd need to use an expression visitor to confirm it's not there.
        // Not checking for now, but could be added as a guard clause.
        var extractedWhereStatement = _container.GetItemLinqQueryable<TRecord>()
            .Where<TRecord>(predicate)
            .ToQueryDefinition().QueryText
            .Split("WHERE")[1];

        var whereStatement = Regex.Replace(extractedWhereStatement, @"root\[""(\w+)""\]", $"{tableAlias}.$1"); 
        return whereStatement;
    }
}


