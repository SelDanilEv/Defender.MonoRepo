using Defender.Common.Consts;
using Microsoft.AspNetCore.Http;

namespace Defender.Common.Helpers;

public static class AuthCookieHelper
{
    public static void SetAuthCookie(HttpResponse response, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        response.Cookies.Append(CookieNames.Authentication, token, BuildCookieOptions());
    }

    public static void ClearAuthCookie(HttpResponse response)
    {
        response.Cookies.Delete(CookieNames.Authentication, BuildCookieOptions());
    }

    public static string? GetAuthToken(HttpRequest request)
    {
        return request.Cookies.TryGetValue(CookieNames.Authentication, out var token)
            && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;
    }

    private static CookieOptions BuildCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        };
    }
}
