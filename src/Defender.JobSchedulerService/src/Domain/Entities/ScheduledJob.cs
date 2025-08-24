using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.JobSchedulerService.Domain.Entities;

public class ScheduledJob : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = String.Empty;
    public string Topic { get; set; } = String.Empty;
    public string Event { get; set; } = String.Empty;
    public Schedule Schedule { get; set; } = new Schedule();

    public ScheduledJob AddSchedule(DateTime startDate, int eachMinute, int eachHour)
    {
        Schedule.NextStartTime = startDate;
        Schedule.LastStartedDate = DateTime.MinValue;
        Schedule.EachMinutes = eachMinute;
        Schedule.EachHour = eachHour;

        return this;
    }

    public bool ScheduleNextRun(bool force = false)
    {
        if (Schedule == null) return false;

        if (!force && Schedule.NextStartTime > DateTime.UtcNow) return false;

        Schedule.LastStartedDate = force
            ? DateTime.UtcNow.AddSeconds(-5)
            : Schedule.NextStartTime;
        Schedule.NextStartTime = Schedule.LastStartedDate;

        while (Schedule.NextStartTime < DateTime.UtcNow)
        {
            Schedule.NextStartTime = Schedule.NextStartTime
                .AddMinutes(Schedule.EachMinutes)
                .AddHours(Schedule.EachHour);
        }

        return true;
    }
}
