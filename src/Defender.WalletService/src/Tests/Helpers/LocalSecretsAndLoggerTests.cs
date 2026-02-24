using Defender.Common.Enums;
using Defender.WalletService.Application.Helpers;
using Defender.WalletService.Application.Helpers.LocalSecretHelper;

namespace Defender.WalletService.Tests.Helpers;

public class LocalSecretsAndLoggerTests
{
    private const string SecretPrefix = "Defender_App_";

    [Fact]
    public async Task GetSecretAsync_WhenSecretEnumProvided_ReturnsEnvironmentValue()
    {
        var secret = Secret.SecretsEncryptionKey;
        var expected = $"value-{Guid.NewGuid():N}";
        var envKey = SecretPrefix + secret;
        Environment.SetEnvironmentVariable(envKey, expected, EnvironmentVariableTarget.Process);

        try
        {
            var value = await LocalSecretsHelper.GetSecretAsync(secret);

            Assert.Equal(expected, value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public async Task GetSecretAsync_WhenLocalSecretProvided_ReturnsEnvironmentValue()
    {
        var secret = (LocalSecret)2001;
        var expected = $"value-{Guid.NewGuid():N}";
        var envKey = SecretPrefix + secret;
        Environment.SetEnvironmentVariable(envKey, expected, EnvironmentVariableTarget.Process);

        try
        {
            var value = await LocalSecretsHelper.GetSecretAsync(secret);

            Assert.Equal(expected, value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public void GetSecretSync_WhenSecretEnumProvided_ReturnsEnvironmentValue()
    {
        var secret = Secret.SharedROConnectionString;
        var expected = $"value-{Guid.NewGuid():N}";
        var envKey = SecretPrefix + secret;
        Environment.SetEnvironmentVariable(envKey, expected, EnvironmentVariableTarget.Process);

        try
        {
            var value = LocalSecretsHelper.GetSecretSync(secret);

            Assert.Equal(expected, value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public void GetSecretSync_WhenLocalSecretProvided_ReturnsEnvironmentValue()
    {
        var secret = (LocalSecret)2002;
        var expected = $"value-{Guid.NewGuid():N}";
        var envKey = SecretPrefix + secret;
        Environment.SetEnvironmentVariable(envKey, expected, EnvironmentVariableTarget.Process);

        try
        {
            var value = LocalSecretsHelper.GetSecretSync(secret);

            Assert.Equal(expected, value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public void Log_WhenExceptionContainsInnerException_DoesNotThrow()
    {
        var exception = new InvalidOperationException("outer", new ArgumentException("inner"));

        var result = Record.Exception(() => SimpleLogger.Log(exception, "custom message"));

        Assert.Null(result);
    }

    [Theory]
    [InlineData(SimpleLogger.LogLevel.Debug)]
    [InlineData(SimpleLogger.LogLevel.Info)]
    [InlineData(SimpleLogger.LogLevel.Warning)]
    public void Log_WhenCalledWithEachLogLevel_DoesNotThrow(SimpleLogger.LogLevel level)
    {
        var result = Record.Exception(() => SimpleLogger.Log("message", level));

        Assert.Null(result);
    }
}
