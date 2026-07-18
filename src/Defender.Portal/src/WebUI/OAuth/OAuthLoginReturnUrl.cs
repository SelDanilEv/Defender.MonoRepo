namespace Defender.Portal.WebUI.OAuth;

public static class OAuthLoginReturnUrl
{
    public static bool IsSafeAuthorizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith("/oauth/authorize", StringComparison.Ordinal)
            && (returnUrl.Length == "/oauth/authorize".Length || returnUrl["/oauth/authorize".Length] == '?');
    }
}
