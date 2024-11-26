﻿using Octokit.Internal;

namespace Showcase.GitHubCopilot.Extensions;
public enum PublicKeyType
{
    /// <summary>
    /// Copilot API public keys for validating request signatures
    /// </summary>
    [Parameter(Value = "copilot_api")]
    CopilotApi,

    /// <summary>
    /// Secret scanning public keys for validating request signatures
    /// </summary>
    [Parameter(Value = "secret_scanning")]
    SecretScanning
}