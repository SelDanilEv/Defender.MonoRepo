using AutoMapper;
using Defender.Common.DB.SharedStorage.Enums;
using Defender.WalletService.Application.DTOs;
using Defender.WalletService.Application.Mappings;
using Defender.WalletService.Domain.Entities.Transactions;
using Defender.WalletService.Domain.Entities.Wallets;
using Defender.WalletService.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defender.WalletService.Tests.Mappings;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<Defender.WalletService.Application.Mappings.MappingProfile>(),
            new NullLoggerFactory());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Config_WhenCreated_IsValid()
    {
        Assert.NotNull(_mapper);
    }

    [Fact]
    public void Map_WalletToWalletDto_MapsOwnerIdCorrectly()
    {
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            WalletNumber = 12345678,
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true)]
        };

        var dto = _mapper.Map<WalletDto>(wallet);

        Assert.Equal(walletId, dto.OwnerId);
        Assert.Equal(12345678, dto.WalletNumber);
    }

    [Fact]
    public void Map_WalletToPublicWalletInfoDto_MapsCurrenciesCorrectly()
    {
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            WalletNumber = 87654321,
            CurrencyAccounts =
            [
                new CurrencyAccount(Currency.USD, true),
                new CurrencyAccount(Currency.EUR, false)
            ]
        };

        var dto = _mapper.Map<PublicWalletInfoDto>(wallet);

        Assert.Equal(walletId, dto.OwnerId);
        Assert.Equal(87654321, dto.WalletNumber);
        Assert.NotNull(dto.Currencies);
        Assert.Contains(Currency.USD, dto.Currencies);
        Assert.Contains(Currency.EUR, dto.Currencies);
    }

    [Fact]
    public void Map_TransactionToTransactionDto_MapsCorrectly()
    {
        var transaction = new Transaction
        {
            TransactionId = "TX-1",
            TransactionType = TransactionType.Payment,
            TransactionStatus = TransactionStatus.Proceed,
            TransactionPurpose = TransactionPurpose.NoPurpose,
            Amount = 100,
            Currency = Currency.USD,
            FromWallet = 11112222
        };

        var dto = _mapper.Map<TransactionDto>(transaction);

        Assert.Equal("TX-1", dto.TransactionId);
        Assert.Equal(TransactionType.Payment, dto.TransactionType);
        Assert.Equal(TransactionStatus.Proceed, dto.TransactionStatus);
        Assert.Equal(100, dto.Amount);
    }

    [Fact]
    public void Map_TransactionToAnonymousTransactionDto_MapsCorrectly()
    {
        var transaction = new Transaction
        {
            TransactionId = "TX-ANON",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Recharge
        };

        var dto = _mapper.Map<AnonymousTransactionDto>(transaction);

        Assert.Equal("TX-ANON", dto.TransactionId);
        Assert.Equal(TransactionStatus.Queued, dto.TransactionStatus);
    }
}
