using Octokit;

namespace Showcase.GitHubCopilot.Extensions;

public class MetaPublicKey
{
    public MetaPublicKey() { }

    public MetaPublicKey(string keyIdentifier, string key, bool isCurrent)
    {
        KeyIdentifier = keyIdentifier;
        Key = key;
        IsCurrent = isCurrent;
    }

    public string KeyIdentifier { get; protected set; }

    public string Key { get; protected set; }

    public bool IsCurrent { get; protected set; }
}


public class MetaPublicKeys
{
    public MetaPublicKeys() { }

    public MetaPublicKeys(IReadOnlyList<MetaPublicKey> publicKeys)
    {
        PublicKeys = publicKeys;
    }

    public IReadOnlyList<MetaPublicKey> PublicKeys { get; protected set; }
}
public class PublicKeysClient : ApiClient
{
    /// <summary>
    /// Initializes a new GitHub Meta Public Keys API client.
    /// </summary>
    /// <param name="apiConnection">An API connection.</param>
    public PublicKeysClient(IApiConnection apiConnection)
        : base(apiConnection)
    {
    }

    /// <summary>
    /// Retrieves public keys for validating request signatures.
    /// </summary>
    /// <exception cref="ApiException">Thrown when a general API error occurs.</exception>
    /// <returns>An <see cref="MetaPublicKeys"/> containing public keys for validating request signatures.</returns>
    [ManualRoute("GET", "/meta/public_keys/{keysType}")]
    public Task<MetaPublicKeys> Get(PublicKeyType keysType)
    {
        return ApiConnection.Get<MetaPublicKeys>(CopilotApiUrls.PublicKeys(keysType));
    }
}
