using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using Showcase.Shared.AIExtensions;

namespace Showcase.GitHubCopilot.Extensions;
public class GitHubAgentFactory : IGitHubAgentFactory
{
    private readonly GitHubCopilotAgentOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAIToolRegistry _aiToolRegistry;

    public GitHubAgentFactory(IOptions<GitHubCopilotAgentOptions> options, IServiceProvider serviceProvider, IAIToolRegistry aiToolRegistry, IEnumerable<IGitHubAgentHandler> agentHandlers)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _aiToolRegistry = aiToolRegistry;
        foreach (var handler in agentHandlers)
        {
            var aiTools = handler.GetAITools();
            _aiToolRegistry.AddRange(aiTools);
        }
    }


    public IChatClient CreateAgent(string accessToken)
    {
        var completionsClient = new OpenAIClient(new(accessToken), new() { Endpoint = new(_options.ApiEndpoint) })
            .AsChatClient(_options.ModelId);
        var innerClient = new ChatClientBuilder(_serviceProvider)
            .UseDistributedCache()
            .UseLogging()
            .UseFunctionInvocation()
            .Use(completionsClient);
        return new GitHubCopilotAgent(innerClient, _aiToolRegistry);
    }
}
