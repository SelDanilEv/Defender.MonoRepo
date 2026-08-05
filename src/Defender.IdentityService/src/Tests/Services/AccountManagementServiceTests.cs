using Defender.Common.Exceptions;
using Defender.IdentityService.Application.Common.Interfaces.Repositories;
using Defender.IdentityService.Application.Common.Interfaces.Services;
using Defender.IdentityService.Application.Helpers;
using Defender.IdentityService.Application.Services;
using Defender.IdentityService.Domain.Entities;
using Moq;

namespace Defender.IdentityService.Tests.Services;

public class AccountManagementServiceTests
{
    private readonly Mock<IAccessCodeService> _accessCodeService = new();
    private readonly Mock<IAccountInfoRepository> _accountInfoRepository = new();
    private readonly Mock<Defender.Common.Interfaces.ICurrentAccountAccessor> _currentAccountAccessor = new();

    private AccountManagementService CreateSut()
        => new(_accessCodeService.Object, _accountInfoRepository.Object, _currentAccountAccessor.Object);

    [Fact]
    public async Task GetAccountByIdAsync_WhenExists_ReturnsAccount()
    {
        var accountId = Guid.NewGuid();
        var account = new AccountInfo { Id = accountId };
        _accountInfoRepository.Setup(x => x.GetAccountInfoByIdAsync(accountId)).ReturnsAsync(account);

        var result = await CreateSut().GetAccountByIdAsync(accountId);

        Assert.Same(account, result);
        Assert.Equal(accountId, result.Id);
    }

    [Fact]
    public async Task GetAccountByIdAsync_WhenNotExists_ReturnsNull()
    {
        var accountId = Guid.NewGuid();
        _accountInfoRepository.Setup(x => x.GetAccountInfoByIdAsync(accountId)).ReturnsAsync((AccountInfo?)null);

        var result = await CreateSut().GetAccountByIdAsync(accountId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAccountWithPasswordAsync_WhenPasswordIncorrect_ThrowsServiceException()
    {
        var accountId = Guid.NewGuid();
        var account = new AccountInfo { Id = accountId, PasswordHash = await PasswordHelper.HashPassword("correct-password") };
        _accountInfoRepository.Setup(x => x.GetAccountInfoByIdAsync(accountId)).ReturnsAsync(account);

        await Assert.ThrowsAsync<ServiceException>(
            () => CreateSut().GetAccountWithPasswordAsync(accountId, "wrong-password"));

        _accountInfoRepository.Verify(
            x => x.UpdateAccountInfoAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<AccountInfo>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAccountWithPasswordAsync_WhenPasswordCorrectAndHashCurrent_ReturnsAccountWithoutRehash()
    {
        var accountId = Guid.NewGuid();
        var account = new AccountInfo { Id = accountId, PasswordHash = await PasswordHelper.HashPassword("correct-password") };
        _accountInfoRepository.Setup(x => x.GetAccountInfoByIdAsync(accountId)).ReturnsAsync(account);

        var result = await CreateSut().GetAccountWithPasswordAsync(accountId, "correct-password");

        Assert.Same(account, result);
        _accountInfoRepository.Verify(
            x => x.UpdateAccountInfoAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<AccountInfo>>()),
            Times.Never);
    }
}
