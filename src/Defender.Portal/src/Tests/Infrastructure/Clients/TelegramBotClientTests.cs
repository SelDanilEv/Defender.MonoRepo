using System.Net;
using System.Text;
using Defender.Portal.Application.Modules.Telegram;
using Defender.Portal.Infrastructure.Clients.Telegram;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Tests.Infrastructure.Clients;

public class TelegramBotClientTests
{
    [Fact]
    public async Task SendMessageAsync_WhenTelegramAcceptsMessage_ReturnsTelegramMessage()
    {
        var handler = new ControlledHandler(_ => JsonResponse(HttpStatusCode.OK, """
            { "ok": true, "result": { "message_id": 91, "chat": { "id": 123456789 } } }
            """));
        var sut = CreateClient(handler);

        var result = await sut.SendMessageAsync(123456789, "Portal is ready.");

        Assert.Equal(91, result.MessageId);
        Assert.Equal(123456789, result.ChatId);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/botbot-token/sendMessage", handler.RequestUri!.AbsolutePath);
        Assert.Equal("{\"chat_id\":123456789,\"text\":\"Portal is ready.\"}", handler.Body);
    }

    [Fact]
    public async Task SendMessageAsync_WhenBotIsBlocked_ThrowsForbiddenExceptionWithoutBotToken()
    {
        const string token = "bot-token";
        var handler = new ControlledHandler(_ => JsonResponse(HttpStatusCode.Forbidden, """
            { "ok": false, "error_code": 403, "description": "Forbidden: bot was blocked by the user" }
            """));
        var sut = CreateClient(handler, token);

        var exception = await Assert.ThrowsAsync<TelegramBotForbiddenException>(() => sut.SendMessageAsync(123456789, "Portal is ready."));

        Assert.Equal(123456789, exception.ChatId);
        Assert.DoesNotContain(token, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(token, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageAsync_WhenTelegramRateLimitsRequest_ThrowsRateLimitExceptionWithRetryAfter()
    {
        var handler = new ControlledHandler(_ => JsonResponse((HttpStatusCode)429, """
            { "ok": false, "error_code": 429, "description": "Too Many Requests", "parameters": { "retry_after": 37 } }
            """));
        var sut = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<TelegramBotRateLimitException>(() => sut.SendMessageAsync(123456789, "Portal is ready."));

        Assert.Equal(TimeSpan.FromSeconds(37), exception.RetryAfter);
    }

    [Fact]
    public async Task SendMessageAsync_WhenTelegramErrorContainsBotToken_RedactsTokenFromException()
    {
        const string token = "bot-token";
        var handler = new ControlledHandler(_ => JsonResponse(HttpStatusCode.BadRequest, $$"""
            { "ok": false, "error_code": 400, "description": "Request to /bot{{token}}/sendMessage failed" }
            """));
        var sut = CreateClient(handler, token);

        var exception = await Assert.ThrowsAsync<TelegramBotApiException>(() => sut.SendMessageAsync(123456789, "Portal is ready."));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.DoesNotContain(token, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(token, exception.ToString(), StringComparison.Ordinal);
    }

    private static TelegramBotClient CreateClient(HttpMessageHandler handler, string botToken = "bot-token") => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") },
        Options.Create(new TelegramOptions { BotToken = botToken }));

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class ControlledHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}
