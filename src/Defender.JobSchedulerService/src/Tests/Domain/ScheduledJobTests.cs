using Defender.JobSchedulerService.Domain.Entities;

namespace Defender.JobSchedulerService.Tests.Domain;

public class ScheduledJobTests
{
    [Fact]
    public void AddSchedule_WhenCalled_UpdatesScheduleFields()
    {
        var startDate = DateTime.UtcNow.AddMinutes(5);
        var job = new ScheduledJob();

        var result = job.AddSchedule(startDate, eachMinute: 15, eachHour: 1);

        Assert.Same(job, result);
        Assert.Equal(startDate, job.Schedule.NextStartTime);
        Assert.Equal(DateTime.MinValue, job.Schedule.LastStartedDate);
        Assert.Equal(15, job.Schedule.EachMinutes);
        Assert.Equal(1, job.Schedule.EachHour);
    }

    [Fact]
    public void ScheduleNextRun_WhenNextStartInFutureAndNotForced_ReturnsFalse()
    {
        var job = new ScheduledJob
        {
            Schedule = new Schedule
            {
                NextStartTime = DateTime.UtcNow.AddMinutes(10),
                EachMinutes = 1,
                EachHour = 0
            }
        };

        var result = job.ScheduleNextRun();

        Assert.False(result);
    }

    [Fact]
    public void ScheduleNextRun_WhenForced_SchedulesNextExecution()
    {
        var job = new ScheduledJob
        {
            Schedule = new Schedule
            {
                NextStartTime = DateTime.UtcNow.AddHours(-1),
                EachMinutes = 10,
                EachHour = 0
            }
        };

        var result = job.ScheduleNextRun(force: true);

        Assert.True(result);
        Assert.True(job.Schedule.LastStartedDate <= DateTime.UtcNow);
        Assert.True(job.Schedule.NextStartTime >= DateTime.UtcNow);
    }
}
