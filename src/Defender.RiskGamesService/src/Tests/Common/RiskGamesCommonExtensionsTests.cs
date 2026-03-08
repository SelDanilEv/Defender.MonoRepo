using Defender.RiskGamesService.Common;
using Defender.RiskGamesService.Common.Kafka;

namespace Defender.RiskGamesService.Tests.Common;

public class RiskGamesCommonExtensionsTests
{
    [Fact]
    public void ServiceName_WhenRead_ReturnsExpectedValue()
    {
        Assert.Equal("RiskGamesService", AppConstants.ServiceName);
    }

    [Fact]
    public void ConsumerGroup_GetName_ReturnsPrefixedGroupName()
    {
        var value = ConsumerGroup.Primary.GetName();

        Assert.Equal("RiskGamesService_Primary-group", value);
    }

    [Fact]
    public void KafkaEventExtensions_GetNameAndToEvent_WorkAsExpected()
    {
        Assert.Equal("StartLotteriesProcessing", KafkaEvent.StartLotteriesProcessing.GetName());
        Assert.Equal(KafkaEvent.ScheduleNewLotteryDraws, "ScheduleNewLotteryDraws".ToEvent());
        Assert.Equal(KafkaEvent.Unknown, "invalid-event-name".ToEvent());
    }

    [Fact]
    public void KafkaTopicExtensions_GetName_ReturnsExpectedTopicOrThrows()
    {
        Assert.Equal("RiskGamesService_scheduled-tasks-topic", KafkaTopic.ScheduledTasks.GetName());
        Assert.Equal("RiskGamesService_lottery-to-process", KafkaTopic.LotteryToProcess.GetName());
        Assert.Throws<ArgumentException>(() => ((KafkaTopic)999).GetName());
    }
}
