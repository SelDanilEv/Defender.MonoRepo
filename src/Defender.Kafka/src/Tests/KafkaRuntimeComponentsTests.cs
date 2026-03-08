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
    public async Task DefaultKafkaProducerProduceAsync_WhenProduceThrows_PropagatesException()
    {
        var producer = new ThrowingProducer();
        var logger = Mock.Of<ILogger<DefaultKafkaProducer<string>>>();
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix(It.IsAny<string>())).Returns("pref-topic");
        var sut = new DefaultKafkaProducer<string>(producer, logger, prefixer.Object);

        await Assert.ThrowsAsync<ProduceException<Null, string>>(
            () => sut.ProduceAsync("topic", "payload", CancellationToken.None));
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
        ConsumerConfig? capturedConfig = null;
        var consumeResult = new ConsumeResult<Ignore, string>
        {
            Message = new Message<Ignore, string> { Value = "hello" },
            TopicPartitionOffset = new TopicPartitionOffset("pref-topic", new Partition(0), new Offset(1))
        };
        consumer.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);
        var sut = new DefaultKafkaConsumer<string>(
            options,
            logger,
            prefixer.Object,
            deserializer,
            (config, _, _) =>
            {
                capturedConfig = config;
                return consumer.Object;
            });
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
        Assert.False(capturedConfig!.EnableAutoCommit);
        consumer.Verify(x => x.Subscribe("pref-topic"), Times.Once);
        consumer.Verify(x => x.Commit(consumeResult), Times.Once);
        consumer.Verify(x => x.Close(), Times.Once);
    }

    [Fact]
    public async Task DefaultKafkaConsumerStartConsuming_WhenHandlerFails_SeeksFailedOffsetAndRetries()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var logger = Mock.Of<ILogger<DefaultKafkaConsumer<string>>>();
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix("group")).Returns("pref-group");
        prefixer.Setup(x => x.AddEnvPrefix("topic")).Returns("pref-topic");
        var deserializer = Mock.Of<IDeserializer<string>>();
        var consumeResult = new ConsumeResult<Ignore, string>
        {
            Message = new Message<Ignore, string> { Value = "hello" },
            TopicPartitionOffset = new TopicPartitionOffset("pref-topic", new Partition(0), new Offset(7))
        };
        var consumer = new Mock<IConsumer<Ignore, string>>();
        consumer.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);
        var sut = new DefaultKafkaConsumer<string>(
            options,
            logger,
            prefixer.Object,
            deserializer,
            (_, _, _) => consumer.Object,
            TimeSpan.Zero);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var attempts = 0;

        await sut.StartConsuming(
            "topic",
            "group",
            _ =>
            {
                attempts++;

                if (attempts == 1)
                {
                    throw new InvalidOperationException("boom");
                }

                cts.Cancel();
                return Task.CompletedTask;
            },
            cts.Token);

        Assert.Equal(2, attempts);
        consumer.Verify(x => x.Seek(consumeResult.TopicPartitionOffset), Times.Once);
        consumer.Verify(x => x.Commit(consumeResult), Times.Once);
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
        prefixer.Setup(x => x.AddEnvPrefix("request-topic")).Returns("env-request-topic");
        prefixer.Setup(x => x.AddEnvPrefix("response-topic")).Returns("env-response-topic");
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());
        var consumer = new Mock<IConsumer<string, string>>();
        var request = new CorrelatedKafkaRequest<string> { CorrelationId = "corr-1", Message = "input" };
        var response = new CorrelatedKafkaResponse<int> { CorrelationId = "corr-1", Message = 42 };
        consumer.SetupSequence(x => x.Consume(It.IsAny<TimeSpan>()))
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
        consumer.Verify(x => x.Subscribe("env-response-topic"), Times.Once);
        producer.Verify(
            x => x.ProduceAsync(
                "env-request-topic",
                It.Is<Message<string, string>>(m => m.Key == "corr-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task KafkaRequestResponseServiceSendAsync_WhenTimeoutReached_ThrowsTimeoutException()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix("group")).Returns("env-group");
        prefixer.Setup(x => x.AddEnvPrefix("request-topic")).Returns("env-request-topic");
        prefixer.Setup(x => x.AddEnvPrefix("response-topic")).Returns("env-response-topic");
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());
        var consumer = new Mock<IConsumer<string, string>>();
        consumer.Setup(x => x.Consume(It.IsAny<TimeSpan>())).Returns((ConsumeResult<string, string>)null!);
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

    private sealed class ThrowingProducer : IProducer<Null, string>
    {
        public Handle Handle => null!;

        public string Name => nameof(ThrowingProducer);

        public int AddBrokers(string brokers) => 0;

        public void SetSaslCredentials(string username, string password)
        {
        }

        public void AbortTransaction()
        {
        }

        public void AbortTransaction(TimeSpan timeout)
        {
        }

        public void BeginTransaction()
        {
        }

        public void CommitTransaction()
        {
        }

        public void CommitTransaction(TimeSpan timeout)
        {
        }

        public void Dispose()
        {
        }

        public void Flush(CancellationToken cancellationToken)
        {
        }

        public int Flush(TimeSpan timeout) => 0;

        public void InitTransactions(TimeSpan timeout)
        {
        }

        public int Poll(TimeSpan timeout) => 0;

        public void Produce(string topic, Message<Null, string> message, Action<DeliveryReport<Null, string>> deliveryHandler)
        {
            throw new NotSupportedException();
        }

        public void Produce(TopicPartition topicPartition, Message<Null, string> message, Action<DeliveryReport<Null, string>> deliveryHandler)
        {
            throw new NotSupportedException();
        }

        public Task<DeliveryResult<Null, string>> ProduceAsync(string topic, Message<Null, string> message, CancellationToken cancellationToken = default)
        {
            throw new ProduceException<Null, string>(
                new Error(ErrorCode.Local_MsgTimedOut),
                new DeliveryResult<Null, string>());
        }

        public Task<DeliveryResult<Null, string>> ProduceAsync(TopicPartition topicPartition, Message<Null, string> message, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout)
        {
        }
    }
}
