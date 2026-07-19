using Defender.Portal.WebUI.Controllers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenIddict.Abstractions;

namespace Defender.Portal.Tests.Controllers;

public class OAuthDynamicClientRegistrationControllerTests
{
    [Fact]
    public async Task RegisterAsync_WhenRegistrationSucceeds_ReturnsRegisteredRedirectUris()
    {
        var controller = new OAuthDynamicClientRegistrationController(Mock.Of<IOpenIddictApplicationManager>());
        var redirectUris = new[] { "http://127.0.0.1:43123/callback" };

        var result = await controller.RegisterAsync(
            new DynamicClientRegistrationRequest(redirectUris, "Codex"),
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(created.Value));

        var returnedRedirectUris = document.RootElement.GetProperty("redirect_uris")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal(redirectUris, returnedRedirectUris);
    }

    [Fact]
    public async Task RegisterAsync_WhenRequestBodyIsMissing_ReturnsInvalidRedirectUri()
    {
        var controller = new OAuthDynamicClientRegistrationController(Mock.Of<IOpenIddictApplicationManager>());

        var result = await controller.RegisterAsync(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("invalid_redirect_uri", badRequest.Value!.GetType().GetProperty("error")!.GetValue(badRequest.Value));
    }
}
