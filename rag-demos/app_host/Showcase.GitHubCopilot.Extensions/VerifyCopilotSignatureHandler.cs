using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace Showcase.GitHubCopilot.Extensions;
public class VerifyCopilotSignatureHandler : AuthorizationHandler<VerifyCopilotSignatureRequirement>
{
    private readonly ILogger<VerifyCopilotSignatureHandler> _logger;
    private readonly GitHubCopilotAgentOptions _agentOptions;
    private const string GitHubTokenHeader = "x-github-token";
    private const string GitHubPublicKeySignatureHeader = "github-public-key-signature";
    private const string GitHubPublicKeyIdentifierHeader = "github-public-key-identifier";

    public VerifyCopilotSignatureHandler(
    ILogger<VerifyCopilotSignatureHandler> logger,
    IOptions<GitHubCopilotAgentOptions> agentOptions)
    {
        _logger = logger;
        _agentOptions = agentOptions.Value;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifyCopilotSignatureRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            _logger.LogWarning("HttpContext is null.");
            context.Fail();
            return;
        }
        httpContext.Request.EnableBuffering();

        if (!string.Equals(httpContext.Request.Method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            context.Fail();
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(GitHubPublicKeyIdentifierHeader, out var publicKeyIdentifier) ||
            !httpContext.Request.Headers.TryGetValue(GitHubPublicKeySignatureHeader, out var publicKeySignature) ||
            !httpContext.Request.Headers.TryGetValue(GitHubTokenHeader, out var token))
        {
            context.Fail();
            return;
        }

        string body;
        using (var reader = new StreamReader(
            httpContext.Request.Body,
            leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
            // Reset the stream position so the next middleware can read it
            httpContext.Request.Body.Position = 0;
        }

        var gitHubClient = new GitHubClient(new ProductHeaderValue(_agentOptions.AgentName, _agentOptions.AgentVersion))
        {
            Credentials = new Credentials(token)
        };
        var bodyIsValid = await gitHubClient.VerifyCopilotRequestByKeyIdAsync(rawBody: body, signature: publicKeySignature!, keyId: publicKeyIdentifier!);

        if (bodyIsValid)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
