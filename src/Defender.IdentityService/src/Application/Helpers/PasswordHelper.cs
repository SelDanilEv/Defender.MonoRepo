using System.Security.Cryptography;
using Defender.IdentityService.Application.Helpers.LocalSecretHelper;
using Defender.Utils;

namespace Defender.IdentityService.Application.Helpers;

public class PasswordHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static Task<string> HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        return Task.FromResult($"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}");
    }

    public static async Task<bool> CheckPassword(string password, string hash)
    {
        if (IsLegacyHash(hash))
            return hash == EncryptionUtils.GetHashSHA256(password, await LocalSecretsHelper.GetSecretAsync(LocalSecret.HashSalt));

        var parts = hash.Split('.');
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, int.Parse(parts[0]), Algorithm, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    // Old hashes are a single SHA-256 digest with no separators; new hashes are "iterations.salt.hash".
    // Lets existing accounts keep logging in so they can be rehashed on next successful login.
    public static bool IsLegacyHash(string hash)
        => hash.Split('.').Length != 3;
}
