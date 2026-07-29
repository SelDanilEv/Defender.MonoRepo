using System.IdentityModel.Tokens.Jwt;
using Defender.Portal.Application.Modules.Telegram;
using Microsoft.Extensions.Configuration;
using CustomClaimTypes = Defender.Common.Consts.ClaimTypes;

namespace Defender.Portal.Tests.Services;

public class TelegramSessionTokenIssuerTests
{
    [Fact]
    public void Issue_WhenLinkedAccountHasRoles_CreatesShortLivedPortalJwt()
    {
        const string secretName = "Defender_App_JwtSecret";
        const string secretValue = "telegram-test-secret-with-at-least-32-bytes";
        var originalSecret = Environment.GetEnvironmentVariable(secretName, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(secretName, secretValue, EnvironmentVariableTarget.Process);
        try
        {
            var now = new DateTimeOffset(2026, 7, 28, 18, 30, 0, TimeSpan.Zero);
            var issuer = new TelegramSessionTokenIssuer(
                new ConfigurationBuilder().AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("JwtTokenIssuer", "Defender"),
                    new KeyValuePair<string, string?>("JwtTokenAudience", "defender-api"),
                ]).Build(),
                new FixedTimeProvider(now));

            var tokenText = issuer.Issue(
                Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066"),
                ["SuperAdmin", "Admin", "User", "Guest"]);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);
            Assert.Equal("Defender", token.Issuer);
            Assert.Contains("defender-api", token.Audiences);
            Assert.Contains(token.Claims, claim => claim.Type == CustomClaimTypes.NameIdentifier && claim.Value == "6da6cfc5-6d8e-448b-9f4c-12d465ed1066");
            Assert.Contains(token.Claims, claim => claim.Type == CustomClaimTypes.Role && claim.Value == "User");
            Assert.DoesNotContain(token.Claims, claim => claim.Type == CustomClaimTypes.Role && claim.Value == "Admin");
            Assert.DoesNotContain(token.Claims, claim => claim.Type == CustomClaimTypes.Role && claim.Value == "SuperAdmin");
            Assert.Contains(token.Claims, claim => claim.Type == "amr" && claim.Value == "telegram");
            Assert.Equal(now.AddHours(1), new DateTimeOffset(token.ValidTo));
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, originalSecret, EnvironmentVariableTarget.Process);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
