using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.Domain.Services;

public static class VehicleOdometerCalculator
{
    public static long? CalculateCurrentOdometer(IEnumerable<ServiceHistoryRecord> records)
    {
        var values = records.Select(record => record.OdometerKm).ToArray();
        return values.Length == 0 ? null : values.Max();
    }

    public static long? Calculate(IEnumerable<ServiceHistoryRecord> records) => CalculateCurrentOdometer(records);

    public static long? GetCurrentOdometer(IEnumerable<ServiceHistoryRecord> records) => CalculateCurrentOdometer(records);

    public static void ValidateChronologicalSequence(IEnumerable<ServiceHistoryRecord> records)
    {
        long? highestEarlierOdometer = null;
        foreach (var group in records.GroupBy(record => record.Date).OrderBy(group => group.Key))
        {
            var lowestCurrentOdometer = group.Min(record => record.OdometerKm);
            if (highestEarlierOdometer is not null && lowestCurrentOdometer < highestEarlierOdometer)
            {
                throw new CarDomainException(CarDomainErrorCodes.OdometerSequenceInvalid, "History odometer decreases across service dates.");
            }

            highestEarlierOdometer = Math.Max(highestEarlierOdometer ?? long.MinValue, group.Max(record => record.OdometerKm));
        }
    }

    public static bool IsChronological(IEnumerable<ServiceHistoryRecord> records)
    {
        try
        {
            ValidateChronologicalSequence(records);
            return true;
        }
        catch (CarDomainException exception) when (exception.Code == CarDomainErrorCodes.OdometerSequenceInvalid)
        {
            return false;
        }
    }

    public static void Validate(IEnumerable<ServiceHistoryRecord> records) => ValidateChronologicalSequence(records);
}
