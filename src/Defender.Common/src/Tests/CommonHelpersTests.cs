using Defender.Common.Cache;
using Defender.Common.Enums;
using Defender.Common.Errors;
using Defender.Common.Extension;
using Microsoft.Extensions.Hosting;

namespace Defender.Common.Tests;

public class CommonHelpersTests
{
    [Fact]
    public void BuildDistributedCacheKey_WhenCalled_ReturnsExpectedFormat()
    {
        var key = CacheConventionBuilder.BuildDistributedCacheKey(
            CacheForService.Portal,
            CacheModel.Wallet,
            "user-1");

        Assert.Equal("Portal_Wallet_user-1", key);
    }

    [Fact]
    public void DigitsOnly_WhenStringContainsMixedCharacters_ReturnsOnlyDigits()
    {
        var result = "abc-12/34 def".DigitsOnly();

        Assert.Equal("1234", result);
    }

    [Fact]
    public void ToErrorCode_WhenUnknownValueProvided_ReturnsUnhandledError()
    {
        var result = ErrorCodeHelper.ToErrorCode("UNKNOWN_CODE");

        Assert.Equal(ErrorCode.UnhandledError, result);
    }

    [Fact]
    public void GetAppEnvironment_WhenDevEnvironmentProvided_ReturnsDev()
    {
        var hostEnvironment = new FakeHostEnvironment("Dev");

        var result = hostEnvironment.GetAppEnvironment();

        Assert.Equal(AppEnvironment.dev, result);
        Assert.True(hostEnvironment.IsLocalOrDevelopment());
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Defender.Common.Tests";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
