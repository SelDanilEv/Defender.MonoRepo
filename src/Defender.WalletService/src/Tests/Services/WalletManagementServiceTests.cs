using Defender.Common.Cache;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.DistributedCache;
using Defender.WalletService.Application.Common.Interfaces.Repositories;
using Defender.WalletService.Application.Services;
using Defender.WalletService.Domain.Entities.Wallets;
using Defender.WalletService.Domain.Enums;
using Moq;
using MongoDB.Driver;

namespace Defender.WalletService.Tests.Services;

public class WalletManagementServiceTests
{
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<IWalletRepository> _repository = new();
    private readonly Mock<ICurrentAccountAccessor> _currentAccountAccessor = new();

    private WalletManagementService CreateSut()
        => new(_cache.Object, _repository.Object, _currentAccountAccessor.Object);

    [Fact]
    public async Task CreateNewWalletAsync_WithoutUserId_UsesCurrentAccountAndUsdDefault()
    {
        var userId = Guid.NewGuid();
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(r => r.CreateNewWalletAsync(It.IsAny<Wallet>()))
            .ReturnsAsync((Wallet w) => w);

        var wallet = await CreateSut().CreateNewWalletAsync();

        Assert.Equal(userId, wallet.Id);
        var account = Assert.Single(wallet.CurrencyAccounts);
        Assert.Equal(Currency.USD, account.Currency);
        Assert.True(account.IsDefault);
        Assert.Equal(0, account.Balance);
    }

    [Fact]
    public async Task AddCurrencyAccountAsync_WhenCurrencyAlreadyExists_Throws()
    {
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true)]
        };

        _repository.Setup(r => r.GetWalletByUserIdAsync(wallet.Id)).ReturnsAsync(wallet);

        await Assert.ThrowsAsync<ServiceException>(() =>
            CreateSut().AddCurrencyAccountAsync(wallet.Id, Currency.USD));
    }

    [Fact]
    public async Task AddCurrencyAccountAsync_WhenNewCurrency_AddsAndUpdates()
    {
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true)]
        };

        _repository.Setup(r => r.GetWalletByUserIdAsync(wallet.Id)).ReturnsAsync(wallet);
        _cache.Setup(c => c.Invalidate(It.IsAny<string>())).Returns(Task.CompletedTask);
        _repository
            .Setup(r => r.UpdateCurrencyAccountsAsync(wallet.Id, It.IsAny<HashSet<CurrencyAccount>>(), It.IsAny<IClientSessionHandle>()))
            .ReturnsAsync((Guid id, HashSet<CurrencyAccount> accounts, IClientSessionHandle _) =>
                new Wallet { Id = id, CurrencyAccounts = accounts });

        var result = await CreateSut().AddCurrencyAccountAsync(wallet.Id, Currency.EUR, isDefault: false);

        Assert.Contains(result.CurrencyAccounts, a => a.Currency == Currency.EUR);
        _repository.Verify(
            r => r.UpdateCurrencyAccountsAsync(wallet.Id, It.Is<HashSet<CurrencyAccount>>(a => a.Any(c => c.Currency == Currency.EUR)), null),
            Times.Once);
    }

    [Fact]
    public async Task SetDefaultCurrencyAccountAsync_WhenCurrencyMissing_Throws()
    {
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true)]
        };

        _repository.Setup(r => r.GetWalletByUserIdAsync(wallet.Id)).ReturnsAsync(wallet);

        await Assert.ThrowsAsync<ServiceException>(() =>
            CreateSut().SetDefaultCurrencyAccountAsync(wallet.Id, Currency.EUR));
    }

    [Fact]
    public async Task SetDefaultCurrencyAccountAsync_WhenCurrencyExists_SetsNewDefaultAndPersists()
    {
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CurrencyAccounts =
            [
                new CurrencyAccount(Currency.USD, true),
                new CurrencyAccount(Currency.EUR, false)
            ]
        };

        _repository.Setup(r => r.GetWalletByUserIdAsync(wallet.Id)).ReturnsAsync(wallet);
        _cache.Setup(c => c.Invalidate(It.IsAny<string>())).Returns(Task.CompletedTask);
        _repository
            .Setup(r => r.UpdateCurrencyAccountsAsync(wallet.Id, It.IsAny<HashSet<CurrencyAccount>>(), It.IsAny<IClientSessionHandle>()))
            .ReturnsAsync((Guid id, HashSet<CurrencyAccount> accounts, IClientSessionHandle _) =>
                new Wallet { Id = id, CurrencyAccounts = accounts });

        var result = await CreateSut().SetDefaultCurrencyAccountAsync(wallet.Id, Currency.EUR);

        Assert.Equal(Currency.EUR, result.CurrencyAccounts.Single(x => x.IsDefault).Currency);
    }

    [Fact]
    public async Task UpdateCurrencyAccountsAsync_InvalidatesCacheAndPersists()
    {
        var walletId = Guid.NewGuid();
        var accounts = new HashSet<CurrencyAccount>
        {
            new(Currency.USD, true),
            new(Currency.EUR, false)
        };

        var expectedKey = CacheConventionBuilder.BuildDistributedCacheKey(
            CacheForService.Portal,
            CacheModel.Wallet,
            walletId.ToString());

        _cache.Setup(c => c.Invalidate(expectedKey)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.UpdateCurrencyAccountsAsync(walletId, accounts, null))
            .ReturnsAsync(new Wallet { Id = walletId, CurrencyAccounts = accounts });

        var result = await CreateSut().UpdateCurrencyAccountsAsync(walletId, accounts);

        Assert.Equal(walletId, result.Id);
        _cache.Verify(c => c.Invalidate(expectedKey), Times.Once);
        _repository.Verify(r => r.UpdateCurrencyAccountsAsync(walletId, accounts, null), Times.Once);
    }
}
