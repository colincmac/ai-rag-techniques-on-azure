#!import ../../utils/csharp/ServiceFromConfig.cs 

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System.Reflection;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Container = Microsoft.Azure.Cosmos.Container;
public class CosmosNoSqlService: ServiceFromConfig<CosmosNoSqlService.Config>
{
    public Database databaseClient;
    public Container containerClient;

    public CosmosNoSqlService(string configFile = DEFAULT_CONFIG_FILE): base(configFile)
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        var client = new CosmosClient(
            connectionString: Configuration.CosmosConnectionString,
            clientOptions: new CosmosClientOptions()
            {
                Serializer = new CosmosSystemTextJsonSerializer(serializerOptions),
                AllowBulkExecution = true
            }
        );
        this.databaseClient = client.GetDatabase(Configuration.CosmosDatabase);
        this.containerClient = this.databaseClient.GetContainer(Configuration.CosmosContainer);
    }
    
    public async Task BulkUpload<T>(IEnumerable<T> items) where T : PartitionedEntity
    {
        var uploadTasks = items.Select(item => containerClient.CreateItemAsync(item, new PartitionKey(item.PartitionKey))).ToAsyncEnumerable();
        await foreach (var task in uploadTasks)
        {
            await task;
        }
    }
    
    public async Task BulkUpload<T>(IAsyncEnumerable<T> items) where T : PartitionedEntity
    {
        await foreach (var item in items)
        {
            ItemResponse<T> itemResponse = null;

            try
            {
                itemResponse = await containerClient.CreateItemAsync(item);
            }
            catch (Exception _)
            {
                Console.WriteLine($"Failed to create item: {JsonSerializer.Serialize(itemResponse)}.");
            }
        }
    }

    public record Config(string CosmosEndpoint, string CosmosKey, string CosmosDatabase, string CosmosContainer, string CosmosConnectionString);
}

public class PartitionedEntity
{
    public string PartitionKey { get; set; }
    public string Type { get; set; }
}

public class CosmosSystemTextJsonSerializer : CosmosLinqSerializer
{
    private readonly JsonObjectSerializer systemTextJsonSerializer;
    private readonly JsonSerializerOptions jsonSerializerOptions;

    public CosmosSystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        this.systemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek
                    && stream.Length == 0)
            {
                return default;
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            return (T)this.systemTextJsonSerializer.Deserialize(stream, typeof(T), default);
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = new MemoryStream();
        this.systemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), default);
        streamPayload.Position = 0;
        return streamPayload;
    }

    public override string SerializeMemberName(MemberInfo memberInfo)
    {
        JsonExtensionDataAttribute jsonExtensionDataAttribute = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(true);
        if (jsonExtensionDataAttribute != null)
        {
            return null;
        }

        JsonPropertyNameAttribute jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true);
        if (!string.IsNullOrEmpty(jsonPropertyNameAttribute?.Name))
        {
            return jsonPropertyNameAttribute.Name;
        }

        if (this.jsonSerializerOptions.PropertyNamingPolicy != null)
        {
            return this.jsonSerializerOptions.PropertyNamingPolicy.ConvertName(memberInfo.Name);
        }

        // Do any additional handling of JsonSerializerOptions here.

        return memberInfo.Name;
    }
}