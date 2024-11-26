using Azure.AI.OpenAI;

namespace Showcase.VoiceRag.Extensions;
public static class OpenAIClientExtensions
{
    public static IVoiceClient AsVoiceClient(this AzureOpenAIClient openAIClient, string modelId) => new VoiceClient(openAIClient, modelId);
}
