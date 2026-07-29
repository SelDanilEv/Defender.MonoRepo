using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Defender.Common.Enums;
using Defender.Common.Helpers;
using Defender.Common.Consts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ClaimTypes = Defender.Common.Consts.ClaimTypes;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramSessionTokenIssuer(IConfiguration configuration, TimeProvider timeProvider) : ITelegramSessionTokenIssuer
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(1);

    public string Issue(Guid accountId, IReadOnlyCollection<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.DateOfBirth, timeProvider.GetUtcNow().ToString("O")),
            new(ClaimTypes.NameIdentifier, accountId.ToString()),
            new("amr", "telegram"),
        };
        claims.AddRange(new[] { Roles.User, Roles.Guest }.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(SecretsHelper.GetSecretSync(Secret.JwtSecret, true)));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = timeProvider.GetUtcNow();
        var token = new JwtSecurityToken(
            configuration["JwtTokenIssuer"],
            configuration["JwtTokenAudience"],
            claims,
            notBefore: now.UtcDateTime,
            expires: now.Add(SessionLifetime).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
