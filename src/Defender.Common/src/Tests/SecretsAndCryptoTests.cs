using System.IdentityModel.Tokens.Jwt;
using Defender.Common.Entities.Secrets;
using Defender.Common.Enums;
using Defender.Common.Helpers;
using Defender.Common.Interfaces;

namespace Defender.Common.Tests;

public class SecretsAndCryptoTests
{
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
}
