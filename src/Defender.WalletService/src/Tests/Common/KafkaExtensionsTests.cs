using Defender.WalletService.Common;
using Defender.WalletService.Common.Kafka;

namespace Defender.WalletService.Tests.Common;

public class KafkaExtensionsTests
{
    [Fact]
    public void ConsumerGroupGetName_WhenCalled_ReturnsServiceScopedName()
    {
        var name = ConsumerGroup.Primary.GetName();

        Assert.Equal($"{AppConstants.ServiceName}_{ConsumerGroup.Primary}-group", name);
    }

    [Fact]
    public void KafkaEventExtensions_WhenKnownValueAndUnknownValueProvided_MapsCorrectly()
    {
        var known = KafkaEvent.StartCacheCleanup.GetName();
        var parsedKnown = known.ToEvent();
        var parsedUnknown = "not-an-event".ToEvent();

        Assert.Equal(KafkaEvent.StartCacheCleanup, parsedKnown);
        Assert.Equal(KafkaEvent.Unknown, parsedUnknown);
    }

    [Fact]
    public void KafkaTopicGetName_WhenKnownTopicProvided_ReturnsMappedName()
    {
        var name = KafkaTopic.TransactionsToProcess.GetName();

        Assert.Equal("WalletService_transactions-to-process-topic", name);
    }

    [Fact]
    public void KafkaTopicGetName_WhenUnknownTopicProvided_ThrowsArgumentException()
    {
        var unknownTopic = (KafkaTopic)999;

        Assert.Throws<ArgumentException>(() => unknownTopic.GetName());
    }
}
