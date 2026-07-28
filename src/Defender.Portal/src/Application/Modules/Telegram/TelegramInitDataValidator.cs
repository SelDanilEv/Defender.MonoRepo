using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramInitDataValidator(TelegramOptions options) : ITelegramInitDataValidator
{
    public TelegramInitData Validate(string rawInitData, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(rawInitData) || string.IsNullOrWhiteSpace(options.BotToken))
        {
            throw new TelegramInitDataValidationException();
        }

        var values = Parse(rawInitData);
        if (!values.Remove("hash", out var suppliedHash) || values.ContainsKey("hash"))
        {
            throw new TelegramInitDataValidationException();
        }

        if (!values.TryGetValue("auth_date", out var authDateText)
            || !long.TryParse(authDateText, out var authDateUnix)
            || !values.TryGetValue("user", out var userJson))
        {
            throw new TelegramInitDataValidationException();
        }

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
        if (authDate > now || now - authDate > options.InitDataMaximumAge)
        {
            throw new TelegramInitDataValidationException();
        }

        var dataCheckString = string.Join("\n", values.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"));
        var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(options.BotToken));
        var expectedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        if (!TryDecodeHex(suppliedHash, out var suppliedHashBytes)
            || suppliedHashBytes.Length != expectedHash.Length
            || !CryptographicOperations.FixedTimeEquals(suppliedHashBytes, expectedHash))
        {
            throw new TelegramInitDataValidationException();
        }

        using var user = JsonDocument.Parse(userJson);
        if (!user.RootElement.TryGetProperty("id", out var id)
            || !id.TryGetInt64(out var telegramUserId))
        {
            throw new TelegramInitDataValidationException();
        }

        var username = user.RootElement.TryGetProperty("username", out var usernameElement)
            ? usernameElement.GetString()
            : null;
        return new TelegramInitData(telegramUserId, username);
    }

    private static Dictionary<string, string> Parse(string rawInitData)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in rawInitData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                throw new TelegramInitDataValidationException();
            }

            var key = Uri.UnescapeDataString(pair[..separator]);
            var value = Uri.UnescapeDataString(pair[(separator + 1)..].Replace('+', ' '));
            if (!result.TryAdd(key, value))
            {
                throw new TelegramInitDataValidationException();
            }
        }

        return result;
    }

    private static bool TryDecodeHex(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
