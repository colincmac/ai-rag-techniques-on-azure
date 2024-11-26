using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenTelemetry.Trace;

namespace Showcase.ServiceDefaults.Clients.AI;
public static class AIServiceExtensions
{
    public static void AddAIServices(this IHostApplicationBuilder hostBuilder, string serviceName, string? chatDeploymentName = null, string? embeddingDeploymentName = null)
    {
        var sourceName = Guid.NewGuid().ToString();
        var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .Build();

        // Configure caching
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        hostBuilder.AddAzureOpenAIClient(serviceName);
        hostBuilder.Services.AddChatClient(c =>
        {
            var openAIClient = c.Services.GetRequiredService<OpenAIClient>();
            var modelId = chatDeploymentName ?? hostBuilder.Configuration["AI:OpenAI:Chat:ModelId"] ?? "gpt-4o";
            return c.UseLogging().UseOpenTelemetry().Use(openAIClient.AsChatClient(modelId));
        });
        hostBuilder.Services.AddEmbeddingGenerator<string, Embedding<float>>(c =>
        {
            var openAIClient = c.Services.GetRequiredService<OpenAIClient>();
            var modelId = embeddingDeploymentName ?? hostBuilder.Configuration["AI:OpenAI:Embedding:ModelId"] ?? "text-embedding-3-large";
            return c.UseLogging().Use(openAIClient.AsEmbeddingGenerator(modelId));
        });
    }

}
