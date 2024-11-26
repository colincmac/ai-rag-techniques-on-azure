using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Showcase.Shared.AIExtensions;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
//JsonSchemaExporterOptions ExporterOptions = new()
//{
//    TreatNullObliviousAsNonNullable = true,
//    TransformSchemaNode = (context, schema) =>
//    {
//        if (schema is not JsonObject jObj)
//        {
//            // Handle the case where the schema is a Boolean.
//            JsonValueKind valueKind = schema.GetValueKind();
//            Debug.Assert(valueKind is JsonValueKind.True or JsonValueKind.False);
//            schema = jObj = new JsonObject();
//            if (valueKind is JsonValueKind.False)
//            {
//                jObj.Add("not", true);
//            }
//        }
//        // Determine if a type or property and extract the relevant attribute provider.
//        ICustomAttributeProvider? attributeProvider = context.PropertyInfo is not null
//            ? context.PropertyInfo.AttributeProvider
//            : context.TypeInfo.Type;
//        var attributeActions = new Dictionary<Type, Action<JsonObject, Attribute>>
//        {
//            { typeof(DescriptionAttribute), (obj, attr) => obj["description"] = ((DescriptionAttribute)attr).Description },
//            { typeof(VectorStoreRecordPartitionKeyAttribute), (obj, attr) => obj["partitionKeyLevel"] = ((VectorStoreRecordPartitionKeyAttribute)attr).HierarchyLevel ?? 0 },
//            { typeof(VectorStoreRecordKeyAttribute), (obj, attr) => obj["isRecordKey"] = true },
//            { typeof(VectorStoreRecordDataAttribute), (obj, attr) => obj["isVectorData"] = true },
//            { typeof(VectorStoreRecordVectorAttribute), (obj, attr) => obj["isVector"] = true },
//            { typeof(VectorStoreEntityAttribute), (obj, attr) => {
//                obj["collectionName"] = ((VectorStoreEntityAttribute)attr).CollectionName;
//                obj["documentType"] = ((VectorStoreEntityAttribute)attr).DocumentType;
//            }}
//        };
//        var attributes = attributeProvider?.GetCustomAttributes(inherit: false);
//        if (attributes != null)
//        {
//            foreach (var attr in attributes)
//            {
//                if (attributeActions.TryGetValue(attr.GetType(), out var action))
//                {
//                    action(jObj, (Attribute)attr);
//                }
//            }
//        }

//        return schema;
//    }
//};
//var serializerOptions = JsonSerializerOptions.Web;
//var exporterOptions = new JsonSchemaExporterOptions
//{
//    TreatNullObliviousAsNonNullable = true,
//};

////JsonNode schema = serializerOptions.GetJsonSchemaAsNode(typeof(ChatThreadMessage), ExporterOptions);
//var schema = VectorStoreEntityJsonSchemaParser.GetVectorRecordSchema<ChatThreadMessage>();
//var schema2 = VectorStoreEntityJsonSchemaParser.GetVectorRecordSchemaAsNode<ChatThreadMessage>();
//Console.WriteLine(JsonSerializer.Serialize(schema2));

//Console.WriteLine(JsonSerializer.Serialize(schema));
//[Description("A person")]
//record Person([property: Description("The name of the person")] string Name);


//[VectorStoreEntity(CollectionName = "chat", DocumentType = "ChatThreadMessage")]
//public record ChatThreadMessage2(
//    [property: VectorStoreRecordPartitionKey] string UserId,
//    [property: VectorStoreRecordPartitionKey] string ThreadId,
//    [property: VectorStoreRecordKey, VectorStoreRecordPartitionKey] string Id
//)
//{
//    public string Type { get; set; } = "ChatThreadMessage";
//    public bool Deleted { get; set; } = false;


//    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
//    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

//    [JsonIgnore]
//    public bool CacheHit { get; set; } = false;

//    [JsonIgnore]
//    public string CacheReferenceId { get; set; } = default;

//    [JsonIgnore]
//    public bool FinishedStream { get; set; } = true;

//    [VectorStoreRecordData]
//    public string? ContextWindow => "MessageContent.Content";

//    [VectorStoreRecordVector]
//    public ReadOnlyMemory<float> Vector { get; set; } = default;

//};
//var constructorInfo = typeof(StreamingChatCompletionsUpdate).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(DateTimeOffset), typeof(string), typeof(CompletionsUsage), typeof(IEnumerable<StreamingChatChoiceUpdate>) }, null);
//var constructorInfo = typeof(StreamingChatCompletionsUpdate).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

//var instance1 = constructorInfo.First().Invoke(new object[] { "id", new DateTimeOffset(), "gpt-4o", null}); // Passing 42 as the constructor parameter
//    var instance2 = constructorInfo.First().Invoke(new object[] { "id", "gpt-4o", new DateTimeOffset(), new Azure.AI.Inference.ChatRole("user"), "World", new object[] {1, 1, 1}, new List<StreamingChatChoiceUpdate>() { new StreamingChatChoiceUpdate()} }); // Passing 42 as the constructor parameter

var streamingupdates = new List<StreamingChatCompletionUpdate>() {
    new StreamingChatCompletionUpdate()
    {
        Text = "Hello",
        RawRepresentation = AIInferenceModelFactory.StreamingChatCompletionsUpdate(choices: [AIInferenceModelFactory.StreamingChatChoiceUpdate(index: 0, delta: AIInferenceModelFactory.StreamingChatResponseMessageUpdate(new Azure.AI.Inference.ChatRole("assistant"), "hello"))])
    },
    new StreamingChatCompletionUpdate()
    {
        Text = "World",
        RawRepresentation = AIInferenceModelFactory.StreamingChatCompletionsUpdate(choices: [AIInferenceModelFactory.StreamingChatChoiceUpdate(index: 0, delta: AIInferenceModelFactory.StreamingChatResponseMessageUpdate(new Azure.AI.Inference.ChatRole("assistant"), "world"))])
    }

};

var stream = new MemoryStream();
await AIJsonUtilitiesExtensions.SerializeAndSendAsSseDataAsync(stream, streamingupdates.Select(x => x.RawRepresentation).ToAsyncEnumerable());
stream.Position = 0;
var reader = new StreamReader(stream);
var serialized = reader.ReadToEnd();
Console.WriteLine(serialized);
