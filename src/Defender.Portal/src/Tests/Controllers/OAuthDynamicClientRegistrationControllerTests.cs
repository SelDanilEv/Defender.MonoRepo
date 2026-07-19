using Defender.Portal.WebUI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenIddict.Abstractions;

namespace Defender.Portal.Tests.Controllers;

public class OAuthDynamicClientRegistrationControllerTests
{
    [Fact]
    public async Task RegisterAsync_WhenRequestBodyIsMissing_ReturnsInvalidRedirectUri()
    {
        var controller = new OAuthDynamicClientRegistrationController(Mock.Of<IOpenIddictApplicationManager>());

        var result = await controller.RegisterAsync(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("invalid_redirect_uri", badRequest.Value!.GetType().GetProperty("error")!.GetValue(badRequest.Value));
    }
}
