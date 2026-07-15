using Defender.Common.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Defender.Common.Helpers;

public static class CryptographyHelper
{
    private const string V2Prefix = "v2";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static async Task<string> EncryptStringAsync(string plainText, string salt = "")
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var key = await GetEncryptionKeyAsync();
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherText = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, cipherText, tag, Encoding.UTF8.GetBytes(salt));

        return string.Join(
            '.',
            V2Prefix,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(cipherText));
    }

    public static async Task<string> DecryptStringAsync(string cipherText, string salt = "")
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        var key = await GetEncryptionKeyAsync();

        return cipherText.StartsWith($"{V2Prefix}.", StringComparison.Ordinal)
            ? DecryptV2(cipherText, key, salt)
            : DecryptLegacy(cipherText, key, salt);
    }

    private static string DecryptV2(string cipherText, byte[] key, string salt)
    {
        var parts = cipherText.Split('.', StringSplitOptions.None);
        if (parts.Length != 4 || parts[0] != V2Prefix)
        {
            throw new CryptographicException("Invalid encrypted payload format.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var encryptedBytes = Convert.FromBase64String(parts[3]);

        if (nonce.Length != NonceSize || tag.Length != TagSize)
        {
            throw new CryptographicException("Invalid encrypted payload format.");
        }

        var plaintextBytes = new byte[encryptedBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, encryptedBytes, tag, plaintextBytes, Encoding.UTF8.GetBytes(salt));

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static string DecryptLegacy(string cipherText, byte[] key, string salt)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = GenerateLegacyIv(Encoding.UTF8.GetBytes(salt));

        using var memoryStream = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);

        return streamReader.ReadToEnd();
    }

    private static async Task<byte[]> GetEncryptionKeyAsync()
    {
        var secret = await SecretsHelper.GetSecretAsync(Secret.SecretsEncryptionKey);

        try
        {
            var key = Convert.FromHexString(secret);
            if (key.Length is not (16 or 24 or 32))
            {
                throw new CryptographicException("Encryption key must be 128, 192, or 256 bits.");
            }

            return key;
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("Encryption key must be a hexadecimal value.", exception);
        }
    }

    private static byte[] GenerateLegacyIv(byte[] salt)
    {
        var iv = new byte[16];

        if (salt.Length == 0)
        {
            return iv;
        }

        for (var i = 0; i < iv.Length; i++)
        {
            iv[i] = salt[i % salt.Length];
        }

        return iv;
    }
}
