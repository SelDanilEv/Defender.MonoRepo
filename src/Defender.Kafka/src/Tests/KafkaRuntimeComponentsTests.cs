using System.Text.Json;
using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.CorrelatedMessage;
using Defender.Kafka.Default;
using Defender.Kafka.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.Kafka.Tests;

public class KafkaRuntimeComponentsTests
{
    [Fact]
    public async Task DefaultKafkaProducerProduceAsync_WhenDeliverySucceeds_UsesPrefixedTopic()
    {
        var producer = new Mock<IProducer<Null, string>>();
        producer
            .Setup(x => x.ProduceAsync(
                "pref-topic",
                It.Is<Message<Null, string>>(m => m.Value == "payload"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<Null, string>
            {
                TopicPartitionOffset = new TopicPartitionOffset("pref-topic", new Partition(0), new Offset(1))
            });
        var logger = Mock.Of<ILogger<DefaultKafkaProducer<string>>>();
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix("topic")).Returns("pref-topic");
        var sut = new DefaultKafkaProducer<string>(producer.Object, logger, prefixer.Object);

        await sut.ProduceAsync("topic", "payload", CancellationToken.None);

        producer.VerifyAll();
    }

    [Fact]
    public async Task DefaultKafkaProducerProduceAsync_WhenProduceThrows_DoesNotPropagateException()
    {
        var producer = new Mock<IProducer<Null, string>>();
        producer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProduceException<Null, string>(
                new Error(ErrorCode.Local_MsgTimedOut),
                new DeliveryResult<Null, string>()));
        var logger = Mock.Of<ILogger<DefaultKafkaProducer<string>>>();
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix(It.IsAny<string>())).Returns("pref-topic");
        var sut = new DefaultKafkaProducer<string>(producer.Object, logger, prefixer.Object);

        await sut.ProduceAsync("topic", "payload", CancellationToken.None);
    }

    [Fact]
    public void DefaultKafkaProducerDispose_WhenCalled_FlushesAndDisposesProducer()
    {
        var producer = new Mock<IProducer<Null, string>>();
        var logger = Mock.Of<ILogger<DefaultKafkaProducer<string>>>();
        var prefixer = Mock.Of<IKafkaEnvPrefixer>();
        var sut = new DefaultKafkaProducer<string>(producer.Object, logger, prefixer);

        sut.Dispose();

        producer.Verify(x => x.Flush(It.IsAny<TimeSpan>()), Times.Once);
        producer.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DefaultKafkaConsumerStartConsuming_WhenMessageConsumed_InvokesHandlerAndClosesConsumer()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var logger = Mock.Of<ILogger<DefaultKafkaConsumer<string>>>();
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix("group")).Returns("pref-group");
        prefixer.Setup(x => x.AddEnvPrefix("topic")).Returns("pref-topic");
        var deserializer = Mock.Of<IDeserializer<string>>();
        var consumer = new Mock<IConsumer<Ignore, string>>();
        consumer.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(new ConsumeResult<Ignore, string> { Message = new Message<Ignore, string> { Value = "hello" } });
        var sut = new DefaultKafkaConsumer<string>(
            options,
            logger,
            prefixer.Object,
            deserializer,
            (_, _, _) => consumer.Object);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        string? consumed = null;

        await sut.StartConsuming(
            "topic",
            "group",
            message =>
            {
                consumed = message;
                cts.Cancel();
                return Task.CompletedTask;
            },
            cts.Token);

        Assert.Equal("hello", consumed);
        consumer.Verify(x => x.Subscribe("pref-topic"), Times.Once);
        consumer.Verify(x => x.Close(), Times.Once);
    }

    [Fact]
    public async Task DefaultKafkaConsumerStartConsuming_WhenTopicInvalid_ThrowsArgumentException()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var logger = Mock.Of<ILogger<DefaultKafkaConsumer<string>>>();
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix(It.IsAny<string>())).Returns(" ");
        var deserializer = Mock.Of<IDeserializer<string>>();
        var sut = new DefaultKafkaConsumer<string>(
            options,
            logger,
            prefixer.Object,
            deserializer,
            (_, _, _) => Mock.Of<IConsumer<Ignore, string>>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.StartConsuming("topic", "group", _ => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task DefaultKafkaConsumerStartConsuming_WhenHandlerIsNull_ThrowsArgumentNullException()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var logger = Mock.Of<ILogger<DefaultKafkaConsumer<string>>>();
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix(It.IsAny<string>())).Returns("pref-value");
        var deserializer = Mock.Of<IDeserializer<string>>();
        var sut = new DefaultKafkaConsumer<string>(
            options,
            logger,
            prefixer.Object,
            deserializer,
            (_, _, _) => Mock.Of<IConsumer<Ignore, string>>());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.StartConsuming("topic", "group", null!, CancellationToken.None));
    }

    [Fact]
    public async Task KafkaRequestResponseServiceSendAsync_WhenMatchingResponseArrives_ReturnsResponsePayload()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix("group")).Returns("env-group");
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());
        var consumer = new Mock<IConsumer<string, string>>();
        var request = new CorrelatedKafkaRequest<string> { CorrelationId = "corr-1", Message = "input" };
        var response = new CorrelatedKafkaResponse<int> { CorrelationId = "corr-1", Message = 42 };
        consumer.SetupSequence(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(new ConsumeResult<string, string> { Message = new Message<string, string> { Key = "another", Value = "{}" } })
            .Returns(new ConsumeResult<string, string> { Message = new Message<string, string> { Key = "corr-1", Value = JsonSerializer.Serialize(response) } });
        ConsumerConfig? capturedConsumerConfig = null;
        var sut = new KafkaRequestResponseService(
            prefixer.Object,
            options,
            _ => producer.Object,
            cfg =>
            {
                capturedConsumerConfig = cfg;
                return consumer.Object;
            });

        var result = await sut.SendAsync<string, int>(
            "request-topic",
            "response-topic",
            "group",
            request,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal("env-group", capturedConsumerConfig!.GroupId);
        producer.Verify(
            x => x.ProduceAsync(
                "request-topic",
                It.Is<Message<string, string>>(m => m.Key == "corr-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task KafkaRequestResponseServiceSendAsync_WhenTimeoutReached_ThrowsTimeoutException()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix(It.IsAny<string>())).Returns("env-group");
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());
        var consumer = new Mock<IConsumer<string, string>>();
        consumer.Setup(x => x.Consume(It.IsAny<CancellationToken>())).Returns((ConsumeResult<string, string>)null!);
        var sut = new KafkaRequestResponseService(
            prefixer.Object,
            options,
            _ => producer.Object,
            _ => consumer.Object);

        await Assert.ThrowsAsync<TimeoutException>(() => sut.SendAsync<string, int>(
            "request-topic",
            "response-topic",
            "group",
            new CorrelatedKafkaRequest<string> { CorrelationId = "corr-timeout", Message = "payload" },
            TimeSpan.FromMilliseconds(25),
            CancellationToken.None));
    }
}
