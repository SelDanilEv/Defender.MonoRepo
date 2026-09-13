using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.Tests.Domain.Validation;

public sealed class InsuranceValidationTests
{
    private static readonly TimeProvider Clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_RejectsProviderAndInvalidDateRange()
    {
        var providerException = Assert.Throws<CarDomainException>(() => InsurancePolicy.Create(Guid.NewGuid(), Guid.NewGuid(), " ", null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), null, Clock));
        var rangeException = Assert.Throws<CarDomainException>(() => InsurancePolicy.Create(Guid.NewGuid(), Guid.NewGuid(), "Insurer", null, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1), null, Clock));

        Assert.Equal("CAR_INSURANCE_PROVIDER_REQUIRED", providerException.Code);
        Assert.Equal("CAR_INSURANCE_DATE_RANGE_INVALID", rangeException.Code);
    }

    [Fact]
    public void Create_RejectsOverlongOptionalFields()
    {
        var exception = Assert.Throws<CarDomainException>(() => InsurancePolicy.Create(Guid.NewGuid(), Guid.NewGuid(), "Insurer", new string('x', 101), null, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), null, Clock));

        Assert.Equal("CAR_INSURANCE_FIELD_TOO_LONG", exception.Code);
    }
}
