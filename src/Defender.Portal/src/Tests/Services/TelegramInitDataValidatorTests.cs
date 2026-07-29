using System.Security.Cryptography;
using System.Text;
using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Tests.Services;

public class TelegramInitDataValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 18, 30, 0, TimeSpan.Zero);
    private const string BotToken = "123456:telegram-test-token";

    [Fact]
    public void Validate_WhenInitDataIsSignedAndFresh_ReturnsTelegramUser()
    {
        var validator = CreateValidator();
        var initData = CreateInitData(Now, "123456789", "danil");

        var result = validator.Validate(initData, Now);

        Assert.Equal(123456789, result.TelegramUserId);
        Assert.Equal("danil", result.Username);
    }

    [Fact]
    public void Validate_WhenAValueIsTampered_ThrowsTelegramInitDataValidationException()
    {
        var validator = CreateValidator();
        var initData = CreateInitData(Now, "123456789", "danil").Replace("danil", "intruder", StringComparison.Ordinal);

        Assert.Throws<TelegramInitDataValidationException>(() => validator.Validate(initData, Now));
    }

    [Fact]
    public void Validate_WhenAuthDateIsOlderThanFiveMinutes_ThrowsTelegramInitDataValidationException()
    {
        var validator = CreateValidator();
        var initData = CreateInitData(Now.AddMinutes(-6), "123456789", "danil");

        Assert.Throws<TelegramInitDataValidationException>(() => validator.Validate(initData, Now));
    }

    [Fact]
    public void Validate_WhenHashIsInvalid_ThrowsTelegramInitDataValidationException()
    {
        var validator = CreateValidator();
        var initData = CreateInitData(Now, "123456789", "danil");
        initData = initData[..^1] + "0";

        Assert.Throws<TelegramInitDataValidationException>(() => validator.Validate(initData, Now));
    }

    private static TelegramInitDataValidator CreateValidator() => new(new TelegramOptions
    {
        BotToken = BotToken,
        InitDataMaximumAge = TimeSpan.FromMinutes(5),
    });

    private static string CreateInitData(DateTimeOffset authDate, string telegramUserId, string username)
    {
        var user = $"{{\"id\":{telegramUserId},\"first_name\":\"Danil\",\"username\":\"{username}\"}}";
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "AAEAAQ",
            ["user"] = user,
        };
        var dataCheckString = string.Join("\n", values.Select(pair => $"{pair.Key}={pair.Value}"));
        var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(BotToken));
        var hash = Convert.ToHexString(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString))).ToLowerInvariant();

        return string.Join("&", values.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}")) + $"&hash={hash}";
    }
}
