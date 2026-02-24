using Defender.UserManagementService.Application.Common.Interfaces.Wrappers;
using Defender.UserManagementService.Application.Services;

namespace Defender.UserManagementService.Tests.Services;

public class AccessCodeServiceTests
{
    [Fact]
    public async Task VerifyUpdateUserAccessCodeAsync_WhenWrapperReturnsTrue_ReturnsTrue()
    {
        var identityWrapper = new Mock<IIdentityWrapper>();
        identityWrapper.Setup(w => w.VerifyUpdateUserAccessCodeAsync(123)).ReturnsAsync(true);
        var sut = new AccessCodeService(identityWrapper.Object);

        var result = await sut.VerifyUpdateUserAccessCodeAsync(123);

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyUpdateUserAccessCodeAsync_WhenWrapperReturnsFalse_ReturnsFalse()
    {
        var identityWrapper = new Mock<IIdentityWrapper>();
        identityWrapper.Setup(w => w.VerifyUpdateUserAccessCodeAsync(456)).ReturnsAsync(false);
        var sut = new AccessCodeService(identityWrapper.Object);

        var result = await sut.VerifyUpdateUserAccessCodeAsync(456);

        Assert.False(result);
    }
}
