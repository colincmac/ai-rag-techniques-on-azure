using Microsoft.Extensions.AI;


namespace Showcase.GitHubCopilot.Extensions;
public interface IGitHubAgentHandler
{
    IEnumerable<AIFunction> GetAITools();
}
