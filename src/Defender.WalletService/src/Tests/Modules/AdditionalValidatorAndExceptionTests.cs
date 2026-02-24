using Defender.WalletService.Application.Common.Exceptions;
using Defender.WalletService.Application.Modules.Transactions.Queries;
using Defender.WalletService.Application.Modules.Wallets.Commands;

namespace Defender.WalletService.Tests.Modules;

public class AdditionalValidatorAndExceptionTests
{
    [Fact]
    public void GetTransactionsQueryValidator_WhenCommandIsDefault_HasNoErrors()
    {
        var validator = new GetTransactionsQueryValidator();
        var command = new GetTransactionHistoryQuery();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddCurrencyAccountCommandValidator_WhenCommandIsDefault_HasNoErrors()
    {
        var validator = new AddCurrencyAccountCommandValidator();
        var command = new AddCurrencyAccountCommand();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetOrCreateWalletCommandValidator_WhenCommandIsDefault_HasNoErrors()
    {
        var validator = new GetOrCreateWalletCommandValidator();
        var command = new GetOrCreateWalletCommand();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetDefaultCurrencyAccountCommandValidator_WhenCommandIsDefault_HasNoErrors()
    {
        var validator = new SetDefaultCurrencyAccountCommandValidator();
        var command = new SetDefaultCurrencyAccountCommand();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CustomException_WhenCreated_ContainsUnhandledErrorCodeMessage()
    {
        var exception = new CustomException();

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }
}
