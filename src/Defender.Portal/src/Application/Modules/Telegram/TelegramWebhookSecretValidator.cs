using System.Security.Cryptography;
using System.Text;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramWebhookSecretValidator(TelegramOptions options) : ITelegramWebhookSecretValidator
{
    public void Validate(string? secret)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret) || string.IsNullOrWhiteSpace(secret))
        {
            throw new TelegramWebhookValidationException();
        }

        var expected = Encoding.UTF8.GetBytes(options.WebhookSecret);
        var supplied = Encoding.UTF8.GetBytes(secret);
        if (expected.Length != supplied.Length || !CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            throw new TelegramWebhookValidationException();
        }
    }
}
