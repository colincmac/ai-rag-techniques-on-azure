using OpenAI.Chat;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Showcase.GitHubCopilot.Extensions;

[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    Converters = [typeof(StreamingChatCompletionUpdateConverter), typeof(JsonModelConverter)]
    )]
[JsonSerializable(typeof(StreamingChatCompletionUpdate))]
public partial class GitHubCopilotJsonContext : JsonSerializerContext
{

};

// For some reason the response from OpenAI is not properly serializing the "Object" property. It always returns chat.completion.chunk, so hardcoding for now
public class StreamingChatCompletionUpdateConverter : JsonModelConverter
{

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(StreamingChatCompletionUpdate);
    }
    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IJsonModel<object> value, JsonSerializerOptions options)
    {
        var data = value.Write(ModelReaderWriterOptions.Json);
        if (data is BinaryData binaryData)
        {
            using var jsonDocument = JsonDocument.Parse(binaryData.ToString());

            writer.WriteStartObject();
            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                if (property.NameEquals("object"))
                {
                    writer.WriteString("object", "chat.completion.chunk"); // Update the property value
                }
                else
                {
                    property.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue(data.ToString());
        }
    }
}
