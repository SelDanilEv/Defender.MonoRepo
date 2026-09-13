using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.Domain.ValueObjects;

public sealed record Cost
{
    public Cost(long? amountMinor, Currency? currency)
    {
        if (amountMinor is null != (currency is null))
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryCostPairInvalid, "Cost amount and currency must be provided together.");
        }

        if (amountMinor < 0 || currency == Enums.Currency.Unknown)
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryCostInvalid, "Cost amount must be non-negative and currency must be supported.");
        }

        AmountMinor = amountMinor;
        Currency = currency;
    }

    public static Cost None { get; } = new(null, null);

    public long? AmountMinor { get; }

    public Currency? Currency { get; }

    public long? CostAmountMinor => AmountMinor;

    public Currency? CostCurrency => Currency;

    public static Cost Create(long? amountMinor, Currency? currency) => new(amountMinor, currency);
}
