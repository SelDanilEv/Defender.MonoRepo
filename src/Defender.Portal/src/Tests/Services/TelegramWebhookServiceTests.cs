using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Tests.Services;

public class TelegramWebhookServiceTests
{
    [Fact]
    public async Task TryRecordAsync_WhenUpdateIsDeliveredTwice_RecordsOnlyFirstReceipt()
    {
        var service = new TelegramWebhookService(new FakeTelegramWebhookReceiptRepository(), new FakeBotClient(), TimeProvider.System);

        var first = await service.TryRecordAsync(77331, CancellationToken.None);
        var duplicate = await service.TryRecordAsync(77331, CancellationToken.None);

        Assert.True(first);
        Assert.False(duplicate);
    }

    [Fact]
    public void Validate_WhenSecretDoesNotMatch_ThrowsTelegramWebhookValidationException()
    {
        var validator = new TelegramWebhookSecretValidator(new TelegramOptions { WebhookSecret = "correct-secret" });

        Assert.Throws<TelegramWebhookValidationException>(() => validator.Validate("wrong-secret"));
    }

    [Fact]
    public async Task HandleAsync_WhenStartCommandReceived_SendsMiniAppLink()
    {
        var bot = new FakeBotClient();
        var service = new TelegramWebhookService(new FakeTelegramWebhookReceiptRepository(), bot, TimeProvider.System);

        await service.HandleAsync(new TelegramWebhookUpdate(77331, new TelegramWebhookMessage(new TelegramWebhookChat(123456789), "/start")), CancellationToken.None);

        var reply = Assert.Single(bot.Messages);
        Assert.Equal(123456789, reply.ChatId);
        Assert.Contains("https://portal.coded-by-danil.dev/telegram", reply.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WhenUnlinkCommandReceived_DirectsUserToMiniAppWithoutUnlinking()
    {
        var bot = new FakeBotClient();
        var service = new TelegramWebhookService(new FakeTelegramWebhookReceiptRepository(), bot, TimeProvider.System);

        await service.HandleAsync(new TelegramWebhookUpdate(77331, new TelegramWebhookMessage(new TelegramWebhookChat(123456789), "/unlink")), CancellationToken.None);

        var reply = Assert.Single(bot.Messages);
        Assert.Contains("Mini App", reply.Text, StringComparison.Ordinal);
        Assert.Contains("https://portal.coded-by-danil.dev/telegram", reply.Text, StringComparison.Ordinal);
    }

    private sealed class FakeTelegramWebhookReceiptRepository : ITelegramWebhookReceiptRepository
    {
        private readonly HashSet<long> updateIds = [];

        public Task<bool> TryCreateAsync(TelegramWebhookReceipt receipt, CancellationToken cancellationToken) =>
            Task.FromResult(updateIds.Add(receipt.UpdateId));
    }

    private sealed class FakeBotClient : ITelegramBotClient
    {
        public List<(long ChatId, string Text)> Messages { get; } = [];

        public Task<TelegramBotMessage> SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            Messages.Add((chatId, text));
            return Task.FromResult(new TelegramBotMessage(chatId, Messages.Count));
        }
    }
}
