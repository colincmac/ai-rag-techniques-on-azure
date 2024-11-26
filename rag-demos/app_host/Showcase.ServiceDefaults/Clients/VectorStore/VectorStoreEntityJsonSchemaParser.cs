using Microsoft.Extensions.VectorData;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Showcase.ServiceDefaults.Clients.VectorStore;

public class VectorStoreEntityPropertyDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool IsRecordKey { get; set; } = false;
    public bool IsVector { get; set; } = false;
    public bool IsVectorData { get; set; } = false;
    public int? PartitionKeyLevel { get; set; }
    public string Description { get; set; } = string.Empty;
}
public class VectorStoreEntityDefinition(string CollectionName, string DocumentType, VectorStoreEntityPropertyDefinition[] PropertyDefinitions)
{
    public string CollectionName { get; init; } = CollectionName;
    public string DocumentType { get; init; } = DocumentType;
    public string IdField => PropertyDefinitions.First(p => p is not null && p.IsRecordKey).Name;

    public string[] AllFields => PropertyDefinitions.Where(p => p.Name is not null).Select(p => p.Name).ToArray();
    public string[] PartitionKeyFields => PropertyDefinitions.Where(p => p.PartitionKeyLevel is not null).OrderBy(o => o.PartitionKeyLevel).Select(p => p.Name).ToArray();
    public string[] VectorDataFields => PropertyDefinitions.Where(p => p is not null && p.IsVectorData).Select(p => p.Name).ToArray();
    public string VectorField => PropertyDefinitions.First(p => p.IsVector).Name;
}
public static class VectorStoreEntityJsonSchemaParser
{
    private static readonly JsonSchemaExporterOptions ExporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = false,
        TransformSchemaNode = (context, schema) =>
        {
            if (schema is not JsonObject jObj)
            {
                // Handle the case where the schema is a Boolean.
                JsonValueKind valueKind = schema.GetValueKind();
                Debug.Assert(valueKind is JsonValueKind.True or JsonValueKind.False);
                schema = jObj = new JsonObject();
                if (valueKind is JsonValueKind.False)
                {
                    jObj.Add("not", true);
                }
            }
            // Determine if a type or property and extract the relevant attribute provider.
            ICustomAttributeProvider? attributeProvider = context.PropertyInfo is not null
                ? context.PropertyInfo.AttributeProvider
                : context.TypeInfo.Type;
            var attributeActions = new Dictionary<Type, Action<JsonObject, Attribute>>
        {
            { typeof(DescriptionAttribute), (obj, attr) => obj["description"] = ((DescriptionAttribute)attr).Description },
            { typeof(VectorStoreRecordPartitionKeyAttribute), (obj, attr) => obj["partitionKeyLevel"] = ((VectorStoreRecordPartitionKeyAttribute)attr).HierarchyLevel ?? 0 },
            { typeof(VectorStoreRecordKeyAttribute), (obj, attr) => obj["isRecordKey"] = true },
            { typeof(VectorStoreRecordDataAttribute), (obj, attr) => obj["isVectorData"] = true },
            { typeof(VectorStoreRecordVectorAttribute), (obj, attr) => obj["isVector"] = true },
            { typeof(VectorStoreEntityAttribute), (obj, attr) => {
                obj["collectionName"] = ((VectorStoreEntityAttribute)attr).CollectionName;
                obj["documentType"] = ((VectorStoreEntityAttribute)attr).DocumentType;
            }}
        };
            var attributes = attributeProvider?.GetCustomAttributes(inherit: false);
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    if (attributeActions.TryGetValue(attr.GetType(), out var action))
                    {
                        action(jObj, (Attribute)attr);
                    }
                }
            }

            return schema;
        }
    };

    public static JsonNode GetVectorRecordSchemaAsNode<TRecord>()
    {
        return JsonSerializerOptions.Web.GetJsonSchemaAsNode(typeof(TRecord), ExporterOptions);
    }

    public static VectorStoreEntityDefinition GetVectorRecordSchema<TRecord>()
    {
        var schemaNode = GetVectorRecordSchemaAsNode<TRecord>() ?? throw new InvalidOperationException($"Could not generate Json Schema for type {typeof(TRecord).Name}");
        var schemaProperties = schemaNode["properties"]?.AsObject()
            .Select(p =>
            {
                var nodeValue = p.Value.Deserialize<VectorStoreEntityPropertyDefinition>(JsonSerializerOptions.Web) ?? throw new InvalidOperationException("Property definition not found");
                nodeValue.Name = p.Key;
                return nodeValue;
            })
            .ToArray() ?? throw new InvalidOperationException($"Json Schema for type {typeof(TRecord).Name} has no properties");

        return new VectorStoreEntityDefinition(
            schemaNode["collectionName"]?.ToString() ?? throw new InvalidOperationException("CollectionName not found"),
            schemaNode["documentType"]?.ToString() ?? throw new InvalidOperationException("DocumentType not found"),
            schemaProperties
        );
    }
}
