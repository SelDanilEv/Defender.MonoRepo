using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Defender.Portal.Application.Modules.Telegram;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Infrastructure.Clients.Telegram;

public sealed class TelegramBotClient(HttpClient httpClient, IOptions<TelegramOptions> options) : ITelegramBotClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TelegramBotMessage> SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        var token = options.Value.BotToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Telegram bot token is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"bot{token}/sendMessage")
        {
            Content = JsonContent.Create(new SendMessageRequest(chatId, text), options: JsonOptions),
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TelegramBotApiResponse>(JsonOptions, cancellationToken);

        if (response.IsSuccessStatusCode && payload is { Ok: true, Result: not null })
        {
            return new TelegramBotMessage(payload.Result.Chat.Id, payload.Result.MessageId);
        }

        throw CreateException(response.StatusCode, chatId, payload?.Parameters?.RetryAfter);
    }

    private static TelegramBotApiException CreateException(HttpStatusCode statusCode, long chatId, int? retryAfter)
    {
        return statusCode switch
        {
            HttpStatusCode.Forbidden => new TelegramBotForbiddenException(chatId),
            (HttpStatusCode)429 => new TelegramBotRateLimitException(TimeSpan.FromSeconds(retryAfter ?? 0)),
            _ => new TelegramBotApiException(statusCode),
        };
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("text")] string Text);

    private sealed record TelegramBotApiResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("result")] TelegramBotApiMessage? Result,
        [property: JsonPropertyName("parameters")] TelegramBotApiParameters? Parameters);

    private sealed record TelegramBotApiMessage(
        [property: JsonPropertyName("message_id")] long MessageId,
        [property: JsonPropertyName("chat")] TelegramBotApiChat Chat);

    private sealed record TelegramBotApiChat([property: JsonPropertyName("id")] long Id);

    private sealed record TelegramBotApiParameters([property: JsonPropertyName("retry_after")] int? RetryAfter);
}
