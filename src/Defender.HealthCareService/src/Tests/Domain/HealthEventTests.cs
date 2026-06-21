using Defender.HealthCareService.Domain.Entities;

namespace Defender.HealthCareService.Tests.Domain;

public class HealthEventTests
{
    [Fact]
    public void HealthEventType_WhenWellbeingRequested_IsAvailable()
    {
        Assert.Equal("Wellbeing", HealthEventType.Wellbeing.ToString());
    }

    [Fact]
    public void HealthEvent_WhenWellbeingScoreAssigned_StoresScore()
    {
        var healthEvent = new HealthEvent
        {
            Type = HealthEventType.Wellbeing,
            WellbeingScore = 5,
        };

        Assert.Equal(5, healthEvent.WellbeingScore);
    }
}
