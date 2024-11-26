using Octokit.Internal;
using System.Globalization;

namespace Showcase.GitHubCopilot.Extensions;
public static class CopilotApiUrls
{
    public static Uri PublicKeys(PublicKeyType keysType)
    {
        return "meta/public_keys/{0}".FormatUri(keysType.ToParameter());
    }

    public static Uri FormatUri(this string pattern, params object[] args)
    {
        var uriString = string.Format(CultureInfo.InvariantCulture, pattern, args).EncodeSharp();

        return new Uri(uriString, UriKind.Relative);
    }
    internal static string EncodeSharp(this string value)
    {
        return !string.IsNullOrEmpty(value) ? value?.Replace("#", "%23") : string.Empty;
    }
    internal static string ToParameter(this Enum prop)
    {
        if (prop == null) return null;

        var propString = prop.ToString();
        var member = prop.GetType().GetMember(propString).FirstOrDefault();

        if (member == null) return null;

        var attribute = member.GetCustomAttributes(typeof(ParameterAttribute), false)
            .Cast<ParameterAttribute>()
            .FirstOrDefault();

        return attribute != null ? attribute.Value : propString.ToLowerInvariant();
    }
}
