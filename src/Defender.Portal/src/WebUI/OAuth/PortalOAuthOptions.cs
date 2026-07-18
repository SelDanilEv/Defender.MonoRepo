using Microsoft.Extensions.Options;

namespace Defender.Portal.WebUI.OAuth;

public sealed class PortalOAuthOptions
{
    public const string SectionName = "PortalOAuth";

    public string Issuer { get; init; } = string.Empty;

    public string McpResourceUri { get; init; } = string.Empty;

    public string McpAudience { get; init; } = string.Empty;

    public string BffAudience { get; init; } = string.Empty;

    public string SigningKeySecretName { get; init; } = "PortalOAuthSigningKey";

    public string EncryptionKeySecretName { get; init; } = "PortalOAuthEncryptionKey";

    public string[] AllowedClientOrigins { get; init; } = [];

    public ValidateOptionsResult Validate()
    {
        if (!IsHttpsUri(Issuer) || !IsHttpsUri(McpResourceUri))
        {
            return ValidateOptionsResult.Fail("Portal OAuth issuer and MCP resource URI must use HTTPS.");
        }

        if (string.IsNullOrWhiteSpace(McpAudience) || string.IsNullOrWhiteSpace(BffAudience))
        {
            return ValidateOptionsResult.Fail("Portal OAuth audiences are required.");
        }

        if (string.IsNullOrWhiteSpace(SigningKeySecretName) || string.IsNullOrWhiteSpace(EncryptionKeySecretName))
        {
            return ValidateOptionsResult.Fail("Portal OAuth key secret names are required.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsHttpsUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
