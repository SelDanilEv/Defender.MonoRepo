namespace Defender.Kafka.Service;

public interface IKafkaEnvPrefixer
{
    string AddEnvPrefix(string entityName);
}
