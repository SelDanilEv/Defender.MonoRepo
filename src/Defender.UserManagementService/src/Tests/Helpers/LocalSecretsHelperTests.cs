using Defender.Common.Enums;
using Defender.UserManagementService.Application.Helpers.LocalSecretHelper;

namespace Defender.UserManagementService.Tests.Helpers;

public class LocalSecretsHelperTests
{
    private const string SecretPrefix = "Defender_App_";

    [Fact]
    public async Task GetSecretAsync_WhenSecretEnumProvided_ReturnsEnvironmentValue()
    {
        var secret = Secret.SharedROConnectionString;
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
        var secret = (LocalSecret)1001;
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
        var secret = Secret.SharedAdminConnectionString;
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
        var secret = (LocalSecret)1002;
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
}
