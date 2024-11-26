using Showcase.Shared.AIExtensions.Realtime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Source-generated JSON type information.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(OpenAIRealtimeExtensions.ConversationFunctionToolParametersSchema))]
internal sealed partial class OpenAIJsonContext : JsonSerializerContext;