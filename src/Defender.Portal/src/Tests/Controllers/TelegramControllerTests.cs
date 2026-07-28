using Defender.Common.Interfaces;
using Defender.Common.Helpers;
using Defender.Common.Consts;
using Defender.Portal.Application.DTOs.Auth;
using Defender.Portal.Application.Modules.Telegram;
using Defender.Portal.WebUI.Controllers.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.Tests.Controllers;

public class TelegramControllerTests
{
    [Fact]
    public async Task CreateSessionAsync_WhenTelegramInitDataIsValid_SetsBffCookieAndReturnsSessionShape()
    {
        var sessions = new Mock<ITelegramSessionService>();
        sessions.Setup(value => value.CreateSessionAsync("signed-init-data", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramSessionResult(new SessionDto { IsAuthenticated = true }, "portal-jwt"));
        var controller = CreateController(sessions.Object);

        var result = await controller.CreateSessionAsync(new TelegramInitDataRequest("signed-init-data"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionDto>(ok.Value);
        Assert.True(session.IsAuthenticated);
        Assert.Contains("__Host-Defender-Telegram=portal-jwt", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateSessionAsync_WhenNormalPortalCookieExists_ClearsItBeforeTelegramCookieIsSet()
    {
        var sessions = new Mock<ITelegramSessionService>();
        sessions.Setup(value => value.CreateSessionAsync("signed-init-data", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramSessionResult(new SessionDto { IsAuthenticated = true }, "portal-jwt"));
        var controller = CreateController(sessions.Object);
        AuthCookieHelper.SetAuthCookie(controller.Response, "normal-portal-jwt");

        await controller.CreateSessionAsync(new TelegramInitDataRequest("signed-init-data"), CancellationToken.None);

        var headers = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains($"{CookieNames.Authentication}=", headers, StringComparison.Ordinal);
        Assert.Contains("expires=", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("__Host-Defender-Telegram=portal-jwt", headers, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReceiveWebhookAsync_WhenUpdateWasAlreadyRecorded_ReturnsSuccessWithoutRepeatingWork()
    {
        var webhook = new Mock<ITelegramWebhookService>();
        webhook.Setup(value => value.TryRecordAsync(77331, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var secret = new Mock<ITelegramWebhookSecretValidator>();
        var controller = CreateController(webhookService: webhook.Object, secretValidator: secret.Object);

        var result = await controller.ReceiveWebhookAsync("expected-secret", new TelegramWebhookUpdate(77331), CancellationToken.None);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ReceiveWebhookAsync_WhenBodyIsMissing_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.ReceiveWebhookAsync("expected-secret", null, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    private static TelegramController CreateController(
        ITelegramSessionService? sessionService = null,
        ITelegramWebhookService? webhookService = null,
        ITelegramWebhookSecretValidator? secretValidator = null)
    {
        var controller = new TelegramController(
            sessionService ?? Mock.Of<ITelegramSessionService>(),
            secretValidator ?? Mock.Of<ITelegramWebhookSecretValidator>(),
            webhookService ?? Mock.Of<ITelegramWebhookService>(),
            Mock.Of<ICurrentAccountAccessor>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        return controller;
    }
}
