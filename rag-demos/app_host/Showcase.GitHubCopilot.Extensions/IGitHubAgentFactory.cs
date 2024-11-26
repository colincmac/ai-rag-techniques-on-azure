using Microsoft.Extensions.AI;

namespace Showcase.GitHubCopilot.Extensions;
public interface IGitHubAgentFactory
{
    IChatClient CreateAgent(string accessToken);
}
