using Octokit;
using System.Security.Cryptography;
using System.Text;

namespace Showcase.GitHubCopilot.Extensions;
public static class GitHubClientExtensions
{
    public static PublicKeysClient PublicKeys(this IGitHubClient client)
    {
        var apiConnection = new ApiConnection(client.Connection);
        return new PublicKeysClient(apiConnection);
    }
    // Verify
    public static bool VerifyRequest(string rawBody, string signature, string key)
    {
        if (string.IsNullOrEmpty(rawBody)) throw new ArgumentException("Invalid payload");
        if (string.IsNullOrEmpty(signature)) throw new ArgumentException("Invalid signature");
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Invalid key");

        try
        {
            using var publicKey = ECDsa.Create();

            byte[] dataBytes = Encoding.UTF8.GetBytes(rawBody);

            byte[] signatureBytes = Convert.FromBase64String(signature);

            var trimmedKey = key.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Replace("\n", "");
            publicKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(trimmedKey), out _);

            var isValid = publicKey.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            return isValid;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> VerifyCopilotRequestByKeyIdAsync(this IGitHubClient client, string rawBody, string signature, string keyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rawBody)) throw new ArgumentException("Invalid payload");
        if (string.IsNullOrEmpty(signature)) throw new ArgumentException("Invalid signature");
        if (string.IsNullOrEmpty(keyId)) throw new ArgumentException("Invalid keyId");

        var publicKeys = await client.PublicKeys().Get(PublicKeyType.CopilotApi);
        var publicKey = publicKeys.PublicKeys.FirstOrDefault(k => k.KeyIdentifier == keyId);

        return publicKey == null
            ? throw new KeyNotFoundException("No public key found matching key identifier")
            : VerifyRequest(rawBody, signature, publicKey.Key);
        ;
    }
}
