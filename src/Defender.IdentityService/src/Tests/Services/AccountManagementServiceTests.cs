using Defender.IdentityService.Application.Common.Interfaces.Repositories;
using Defender.IdentityService.Application.Common.Interfaces.Services;
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
}
