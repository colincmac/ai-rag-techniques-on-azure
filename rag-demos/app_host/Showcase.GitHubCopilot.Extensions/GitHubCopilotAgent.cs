using Microsoft.Extensions.AI;
using Showcase.Shared.AIExtensions;
using System.Text.RegularExpressions;

namespace Showcase.GitHubCopilot.Extensions;

public partial class GitHubCopilotAgent : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly IAIToolRegistry _toolRegistry;

    public string? Name { get; set; }

    public GitHubCopilotAgent(IChatClient client, IAIToolRegistry toolRegistry, string? name = default)
    {
        _innerClient = client;
        _toolRegistry = toolRegistry;
        Name = name;
    }

    public ChatClientMetadata Metadata => _innerClient.Metadata;


    [GeneratedRegex(@"\[\s*""?\/(\S+)"",?\s*""(.*)""\s*\]")]
    public static partial Regex MessageRegexAfterIntent();

    [GeneratedRegex(@"(@[a-zA-Z0-9_-]+)?\s*(?:/(?<command>[a-zA-Z0-9_-]+))?\s*(?<content>.*)")]
    public static partial Regex MessageRegexBeforeFormatDetection();

    public Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var chatOptions = OptionsWithTools(chatMessages, options);
        return _innerClient.CompleteAsync(chatMessages, chatOptions, cancellationToken);
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var chatOptions = OptionsWithTools(chatMessages, options);
        return _innerClient.CompleteStreamingAsync(chatMessages, chatOptions, cancellationToken);
    }

    public TService? GetService<TService>(object? key = null) where TService : class
    {
        return key is null && this is TService service ? service : _innerClient.GetService<TService>(key);
    }

    private ChatOptions? OptionsWithTools(IList<ChatMessage> chatMessages, ChatOptions? options)
    {
        var (tools, command, content) = ParseMessage(chatMessages);
        if (tools.Count > 0)
        {
            var specificToolRequested = tools.Count == 1 && !string.IsNullOrEmpty(command);

            options ??= new();
            options.ToolMode = specificToolRequested ? ChatToolMode.RequireSpecific(tools.First().Metadata.Name) : ChatToolMode.Auto;
            options.Tools = options.Tools is null ? [.. tools] : [.. options.Tools, .. tools];

        }
        return options;
    }

    public (List<AIFunction> Tools, string? command, string? content) ParseMessage(IEnumerable<ChatMessage> messages)
    {
        var lastMessage = messages?.LastOrDefault() ?? throw new InvalidOperationException("Copilot chat input message is null");

        if (lastMessage?.Role != ChatRole.User || string.IsNullOrEmpty(lastMessage.Text))
        {
            // If the last message's role is not User. This is not a message from user input.
            throw new InvalidOperationException("The message is not input from a user or the message is empty");
        }

        var match = MessageRegexBeforeFormatDetection().Match(lastMessage.Text);
        var tools = _toolRegistry.ToList();
        if (match.Groups[2].Success && _toolRegistry.TryGetTool(match.Groups[2].Value, out var tool))
        {
            // only 1 tool is requested
            tools = [tool];
        }

        return (tools, match.Groups[2].Value, match.Groups[3].Value);
    }


    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerClient.Dispose();
        }
    }
}
