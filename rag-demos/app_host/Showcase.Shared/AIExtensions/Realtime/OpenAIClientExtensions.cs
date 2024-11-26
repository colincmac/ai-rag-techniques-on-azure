using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;

namespace Showcase.Shared.AIExtensions.Realtime;
public static class OpenAIClientExtensions
{
    public static IVoiceClient AsVoiceClient(this AzureOpenAIClient openAIClient, string modelId, ILogger logger) => new VoiceClient(openAIClient, modelId, logger);
}
