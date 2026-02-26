using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.CorrelatedMessage;
using Defender.Kafka.Default;
using Defender.Kafka.Extension;
using Defender.Kafka.Serialization;
using Defender.Kafka.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.Kafka.Tests;

public class KafkaRegistrationAndConstructorTests
{
    [Fact]
    public void AddKafka_WhenCalled_RegistersExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddKafka(options => options.BootstrapServers = "localhost:9092");

        Assert.Contains(services, x => x.ServiceType == typeof(IKafkaEnvPrefixer));
        Assert.Contains(services, x => x.ServiceType == typeof(IKafkaRequestResponseService));
        Assert.Contains(services, x => x.ServiceType == typeof(IDefaultKafkaProducer<>));
        Assert.Contains(services, x => x.ServiceType == typeof(IDefaultKafkaConsumer<>));
        Assert.Contains(services, x => x.ServiceType == typeof(ISerializer<>));
        Assert.Contains(services, x => x.ServiceType == typeof(IDeserializer<>));
    }

    [Fact]
    public void DefaultKafkaProducerConstructor_WhenBootstrapServersMissing_ThrowsArgumentNullException()
    {
        var logger = Mock.Of<ILogger<DefaultKafkaProducer<string>>>();
        var prefixer = Mock.Of<IKafkaEnvPrefixer>();
        var serializer = Mock.Of<ISerializer<string>>();

        Assert.Throws<ArgumentNullException>(() => new DefaultKafkaProducer<string>(null!, logger, prefixer, serializer));
    }

    [Fact]
    public void DefaultKafkaConsumerConstructor_WhenBootstrapServersMissing_ThrowsArgumentException()
    {
        var options = Options.Create(new KafkaOptions());
        var logger = Mock.Of<ILogger<DefaultKafkaConsumer<string>>>();
        var prefixer = Mock.Of<IKafkaEnvPrefixer>();
        var serializer = Mock.Of<IDeserializer<string>>();

        Assert.Throws<ArgumentException>(() => new DefaultKafkaConsumer<string>(options, logger, prefixer, serializer));
    }

    [Fact]
    public void DefaultKafkaConsumerConstructor_WhenLoggerMissing_ThrowsArgumentNullException()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var prefixer = Mock.Of<IKafkaEnvPrefixer>();
        var serializer = Mock.Of<IDeserializer<string>>();

        Assert.Throws<ArgumentNullException>(() => new DefaultKafkaConsumer<string>(options, null!, prefixer, serializer));
    }

    [Fact]
    public void DefaultKafkaProducerConstructor_WhenSerializerMissing_ThrowsArgumentNullException()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var logger = Mock.Of<ILogger<DefaultKafkaProducer<string>>>();
        var prefixer = Mock.Of<IKafkaEnvPrefixer>();

        Assert.Throws<ArgumentNullException>(() => new DefaultKafkaProducer<string>(options, logger, prefixer, null!));
    }
}
