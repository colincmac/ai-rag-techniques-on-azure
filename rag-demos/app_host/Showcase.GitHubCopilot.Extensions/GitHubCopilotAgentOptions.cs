namespace Showcase.GitHubCopilot.Extensions;
public class GitHubCopilotAgentOptions
{
    public const string ConfigurationSection = "GitHubCopilotAgent";

    public string AgentName { get; set; } = "ColinAIDev";
    public string ModelId { get; set; } = "gpt-4o";
    public string ApiEndpoint { get; set; } = "https://api.githubcopilot.com";
    public string? AgentVersion { get; set; } = "1.0.0";

}
