using Confluent.Kafka;
using Defender.Kafka.CorrelatedMessage;
using Defender.Kafka.Service;
using Defender.Kafka.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Defender.Kafka.Tests;

public class KafkaHelpersTests
{
    [Theory]
    [InlineData("Production", "prod_topic")]
    [InlineData("DockerProd", "prod_topic")]
    [InlineData("Dev", "dev_topic")]
    [InlineData("DockerDev", "dev_topic")]
    [InlineData("Local", "local_topic")]
    [InlineData("SomethingElse", "local_topic")]
    public void AddEnvPrefix_WhenEnvironmentProvided_ReturnsMappedPrefix(string environmentName, string expected)
    {
        var env = new FakeHostEnvironment { EnvironmentName = environmentName };
        var prefixer = new KafkaEnvPrefixer(env);

        var result = prefixer.AddEnvPrefix("topic");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CorrelatedKafkaRequestCreateDefault_WhenCalled_ReturnsEmptyMessage()
    {
        var request = CorrelatedKafkaRequest.CreateDefault;

        Assert.Equal(string.Empty, request.Message);
        Assert.False(string.IsNullOrWhiteSpace(request.CorrelationId));
    }

    [Fact]
    public void CorrelatedKafkaRequestCreateResponse_WhenCalled_CopiesCorrelationAndMessage()
    {
        var request = new CorrelatedKafkaRequest<int> { CorrelationId = "corr-1", Message = 10 };

        var response = request.CreateResponse("ok");

        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal("ok", response.GetResult);
    }

    [Fact]
    public void JsonSerializerSerializeAndDeserialize_WhenValidObjectProvided_RoundTripsObject()
    {
        var logger = new Mock<ILogger<JsonSerializer<TestMessage>>>();
        var serializer = new JsonSerializer<TestMessage>(logger.Object);
        var source = new TestMessage { Id = 5, Name = "payload" };

        var bytes = serializer.Serialize(source, SerializationContext.Empty);
        var result = serializer.Deserialize(bytes, isNull: false, SerializationContext.Empty);

        Assert.NotNull(result);
        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.Name, result.Name);
    }

    [Fact]
    public void JsonSerializerDeserialize_WhenInvalidJsonProvided_ReturnsDefault()
    {
        var logger = new Mock<ILogger<JsonSerializer<TestMessage>>>();
        var serializer = new JsonSerializer<TestMessage>(logger.Object);

        var result = serializer.Deserialize("not-json"u8.ToArray(), isNull: false, SerializationContext.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void JsonSerializerSerialize_WhenJsonExceptionOccurs_Throws()
    {
        var logger = new Mock<ILogger<JsonSerializer<LoopMessage>>>();
        var serializer = new JsonSerializer<LoopMessage>(logger.Object);
        var payload = new LoopMessage();
        payload.Self = payload;

        Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() => serializer.Serialize(payload, SerializationContext.Empty));
    }

    public sealed class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class LoopMessage
    {
        public LoopMessage? Self { get; set; }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Local";
        public string ApplicationName { get; set; } = "Defender.Kafka.Tests";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
