using Defender.CarService.Domain.Entities;

namespace Defender.CarService.Domain.ValueObjects;

public sealed record EffectiveBaseline
{
    public EffectiveBaseline(DateOnly? date, long? odometerKm, Guid? recordId = null)
    {
        Date = date;
        OdometerKm = odometerKm;
        RecordId = recordId;
    }

    public DateOnly? Date { get; }

    public long? OdometerKm { get; }

    public Guid? RecordId { get; }

    public DateOnly? LastDate => Date;

    public long? LastOdometerKm => OdometerKm;

    public static EffectiveBaseline FromHistory(IEnumerable<ServiceHistoryRecord> records, ManualBaseline manualBaseline)
    {
        var latest = records
            .OrderByDescending(record => record.Date)
            .ThenByDescending(record => record.OdometerKm)
            .ThenBy(record => record.Id)
            .FirstOrDefault();

        return latest is null
            ? new EffectiveBaseline(manualBaseline.Date, manualBaseline.OdometerKm)
            : new EffectiveBaseline(latest.Date, latest.OdometerKm, latest.Id);
    }

    public static EffectiveBaseline FromHistory(IEnumerable<ServiceHistoryRecord> records, DateOnly? manualDate, long? manualOdometerKm)
        => FromHistory(records, new ManualBaseline(manualDate, manualOdometerKm));
}
