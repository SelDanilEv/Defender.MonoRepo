using Defender.Kafka;

namespace Defender.Kafka.Tests;

public class TopicExtensionsTests
{
    [Fact]
    public void GetName_WhenKnownTopicProvided_ReturnsMappedName()
    {
        var name = Topic.DistributedCache.GetName();

        Assert.Equal("distributed-cache", name);
    }

    [Fact]
    public void ToTopic_WhenKnownNameProvided_ReturnsMappedTopic()
    {
        var topic = "transaction-status-updates".ToTopic();

        Assert.Equal(Topic.TransactionStatusUpdates, topic);
    }

    [Fact]
    public void ToTopic_WhenUnknownNameProvided_ThrowsArgumentException()
    {
        Action act = () => _ = "unknown-topic".ToTopic();

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Unknown topic", exception.Message);
    }

    [Fact]
    public void GetName_WhenUnknownEnumValueProvided_ThrowsArgumentException()
    {
        Action act = () => _ = ((Topic)999).GetName();

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Unknown topic", exception.Message);
    }
}
