using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Defender.Common.Entities.Secrets;
using Defender.Common.Enums;
using Defender.Common.Helpers;
using Defender.Common.Interfaces;

namespace Defender.Common.Tests;

public class SecretsAndCryptoTests
{
    [Fact]
    public async Task EncryptStringAsync_WhenCalledTwice_UsesDifferentVersionedPayloads()
    {
        Environment.SetEnvironmentVariable(
            "Defender_App_SecretsEncryptionKey",
            "00112233445566778899AABBCCDDEEFF",
            EnvironmentVariableTarget.Process);

        var first = await CryptographyHelper.EncryptStringAsync("secret-payload", "secret-name");
        var second = await CryptographyHelper.EncryptStringAsync("secret-payload", "secret-name");

        Assert.StartsWith("v2.", first);
        Assert.StartsWith("v2.", second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task DecryptStringAsync_WhenV2PayloadWasModified_ThrowsCryptographicException()
    {
        Environment.SetEnvironmentVariable(
            "Defender_App_SecretsEncryptionKey",
            "00112233445566778899AABBCCDDEEFF",
            EnvironmentVariableTarget.Process);
        var encrypted = await CryptographyHelper.EncryptStringAsync("secret-payload", "secret-name");
        var parts = encrypted.Split('.');
        var cipherText = Convert.FromBase64String(parts[3]);
        cipherText[0] ^= 0x01;
        parts[3] = Convert.ToBase64String(cipherText);

        await Assert.ThrowsAnyAsync<CryptographicException>(() =>
            CryptographyHelper.DecryptStringAsync(string.Join('.', parts), "secret-name"));
    }

    [Fact]
    public async Task DecryptStringAsync_WhenLegacyPayloadProvided_ReturnsPlainText()
    {
        Environment.SetEnvironmentVariable(
            "Defender_App_SecretsEncryptionKey",
            "00112233445566778899AABBCCDDEEFF",
            EnvironmentVariableTarget.Process);
        var legacy = EncryptLegacyForTest("secret-payload", "secret-name");

        var result = await CryptographyHelper.DecryptStringAsync(legacy, "secret-name");

        Assert.Equal("secret-payload", result);
    }

    [Fact]
    public async Task GetSecretAsync_WhenKeyIsEmpty_ReturnsEmptyString()
    {
        var result = await SecretsHelper.GetSecretAsync(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetSecretAsync_WhenProcessEnvironmentVariableExists_ReturnsValue()
    {
        var key = $"UnitTest_{Guid.NewGuid():N}";
        var envKey = $"Defender_App_{key}";
        Environment.SetEnvironmentVariable(envKey, "env-secret", EnvironmentVariableTarget.Process);

        var result = await SecretsHelper.GetSecretAsync(key, useMongoSecrets: false);

        Assert.Equal("env-secret", result);
    }

    [Fact]
    public async Task GetSecretAsync_WhenMissingInEnvironment_UsesMongoAccessor()
    {
        var key = $"UnitTest_{Guid.NewGuid():N}";
        var accessor = new Mock<IMongoSecretAccessor>();
        accessor.Setup(x => x.GetSecretValueByNameAsync(key)).ReturnsAsync("mongo-secret");
        accessor.Setup(x => x.GetSecretByNameAsync(It.IsAny<string>())).ReturnsAsync(new MongoSecret());
        SecretsHelper.Initialize(accessor.Object);

        var result = await SecretsHelper.GetSecretAsync(key, useMongoSecrets: true);

        Assert.Equal("mongo-secret", result);
    }

    [Fact]
    public async Task EncryptAndDecryptStringAsync_WhenValidSecretConfigured_RoundTripsValue()
    {
        Environment.SetEnvironmentVariable(
            "Defender_App_SecretsEncryptionKey",
            "00112233445566778899AABBCCDDEEFF",
            EnvironmentVariableTarget.Process);
        var plainText = "secret-payload";
        var salt = "salt";

        var encrypted = await CryptographyHelper.EncryptStringAsync(plainText, salt);
        var decrypted = await CryptographyHelper.DecryptStringAsync(encrypted, salt);

        Assert.NotEqual(plainText, encrypted);
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public async Task GenerateInternalJwtAsync_WhenIssuerProvided_ReturnsTokenWithExpectedClaims()
    {
        Environment.SetEnvironmentVariable(
            "Defender_App_JwtSecret",
            "0123456789ABCDEF0123456789ABCDEF",
            EnvironmentVariableTarget.Process);

        var token = await InternalJwtHelper.GenerateInternalJWTAsync("issuer-under-test");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("issuer-under-test", parsed.Issuer);
        Assert.Contains(parsed.Claims, x => x.Type == Defender.Common.Consts.ClaimTypes.NameIdentifier);
        Assert.Contains(parsed.Claims, x => x.Type == Defender.Common.Consts.ClaimTypes.Role);
    }

    [Fact]
    public async Task GenerateInternalJwtAsync_WhenIssuerProvided_AddsSharedAudience()
    {
        const string jwtSecretEnvironmentVariable = "Defender_App_JwtSecret";
        var previousJwtSecret = Environment.GetEnvironmentVariable(
            jwtSecretEnvironmentVariable,
            EnvironmentVariableTarget.Process);

        try
        {
            Environment.SetEnvironmentVariable(
                jwtSecretEnvironmentVariable,
                "0123456789ABCDEF0123456789ABCDEF",
                EnvironmentVariableTarget.Process);

            var token = await InternalJwtHelper.GenerateInternalJWTAsync("issuer-under-test");
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Contains("defender-api", parsed.Audiences);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                jwtSecretEnvironmentVariable,
                previousJwtSecret,
                EnvironmentVariableTarget.Process);
        }
    }

    private static string EncryptLegacyForTest(string plainText, string salt)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromHexString("00112233445566778899AABBCCDDEEFF");
        var saltBytes = Encoding.UTF8.GetBytes(salt);
        aes.IV = Enumerable.Range(0, 16)
            .Select(index => saltBytes[index % saltBytes.Length])
            .ToArray();

        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var streamWriter = new StreamWriter(cryptoStream))
        {
            streamWriter.Write(plainText);
        }

        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
