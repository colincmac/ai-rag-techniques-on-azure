using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using System.Reflection;


namespace Showcase.GitHubCopilot.Extensions;
public class GitHubAgentFactory : IGitHubAgentFactory
{
    private readonly GitHubCopilotAgentOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, IList<AITool>> _aIFunctionLookup = new();
    public GitHubAgentFactory(IOptions<GitHubCopilotAgentOptions> options, IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        var agentHandlers = _serviceProvider.GetServices<IGitHubAgentHandler>();
        foreach(var handler in agentHandlers)
        {
            var aiTools = handler.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => (AITool)AIFunctionFactory.Create(m, handler, null))
                .ToList();

            if(aiTools.Count > 0)
            {
                _aIFunctionLookup.TryAdd(handler.SlashCommand, aiTools);
            }
        }
    }

    public IChatClient CreateAgent(string accessToken, string? slashCommand = default)
    {
        var aiFunctions = default(IList<AITool>);
        if (slashCommand is not null) _aIFunctionLookup.TryGetValue(slashCommand, out aiFunctions);


        var completionsClient = new OpenAIClient(new(accessToken), new() { Endpoint = new(_options.ApiEndpoint) })
            .AsChatClient(_options.ModelId);
        var innerClient = new ChatClientBuilder(_serviceProvider)
            .UseDistributedCache()
            .UseLogging()
            .UseFunctionInvocation()
            .Use(completionsClient);
        return new GitHubCopilotAgent(innerClient, aiFunctions);
    }
}
