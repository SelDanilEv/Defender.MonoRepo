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

    [Fact]
    public void HealthEventType_WhenAnalysisRequested_IsAvailable()
    {
        Assert.Equal("Analysis", HealthEventType.Analysis.ToString());
    }

    [Fact]
    public void HealthEvent_WhenAnalysisAssigned_StoresAnalysisFields()
    {
        var healthEvent = new HealthEvent
        {
            Type = HealthEventType.Analysis,
            AnalysisName = "CRP",
            AnalysisStatus = AnalysisStatus.Excellent,
        };

        Assert.Equal("CRP", healthEvent.AnalysisName);
        Assert.Equal(AnalysisStatus.Excellent, healthEvent.AnalysisStatus);
    }
}
