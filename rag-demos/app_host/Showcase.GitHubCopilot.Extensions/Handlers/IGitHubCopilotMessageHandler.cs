namespace Showcase.GitHubCopilot.Extensions.Handlers;
public interface IGitHubCopilotMessageHandler
{
    Task HandleMessage(CancellationToken cancellationToken);
}
