namespace Defender.JobSchedulerService.Domain.Entities;

public record Schedule
{
    public DateTime NextStartTime { get; set; }
    public DateTime LastStartedDate { get; set; }
    public int EachMinutes { get; set; }
    public int EachHour { get; set; }
}
