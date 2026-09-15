using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.Domain.ValueObjects;

public sealed record ManualBaseline
{
    public ManualBaseline(DateOnly? date, long? odometerKm)
    {
        if (odometerKm < 0)
        {
            throw new CarDomainException(CarDomainErrorCodes.MaintenanceBaselineInvalid, "Manual baseline odometer cannot be negative.");
        }

        Date = date;
        OdometerKm = odometerKm;
    }

    public static ManualBaseline Empty { get; } = new(null, null);

    public DateOnly? Date { get; }

    public long? OdometerKm { get; }

    public DateOnly? ManualBaselineDate => Date;

    public long? ManualBaselineOdometerKm => OdometerKm;
}
