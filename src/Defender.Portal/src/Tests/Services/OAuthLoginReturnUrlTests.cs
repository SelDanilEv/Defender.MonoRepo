using Defender.Portal.WebUI.OAuth;

namespace Defender.Portal.Tests.Services;

public class OAuthLoginReturnUrlTests
{
    [Theory]
    [InlineData("/oauth/authorize?client_id=defender-mcp", true)]
    [InlineData("https://attacker.example/oauth/authorize", false)]
    [InlineData("/home", false)]
    public void IsSafeAuthorizeReturnUrl_WhenPathIsChecked_ReturnsExpectedResult(string returnUrl, bool expected)
    {
        Assert.Equal(expected, OAuthLoginReturnUrl.IsSafeAuthorizeReturnUrl(returnUrl));
    }
}
