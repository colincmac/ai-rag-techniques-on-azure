using Microsoft.AspNetCore.Authorization;

namespace Showcase.GitHubCopilot.Extensions;
public class VerifyCopilotSignatureRequirement : IAuthorizationRequirement
{
    public const string RequirementName = "VerifyCopilotSignature";
}
