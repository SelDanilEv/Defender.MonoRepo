using Microsoft.Extensions.Hosting;

namespace Defender.Kafka.Service;

internal class KafkaEnvPrefixer(
        IHostEnvironment hostEnvironment)
    : IKafkaEnvPrefixer
{
    public string AddEnvPrefix(string entityName)
    {
        var env = hostEnvironment.EnvironmentName switch
        {
            "Production" or "DockerProd" or "Prod" => "prod",
            "Development" or "DockerDev" or "Dev" => "dev",
            "Local" or "DockerLocal" => "local",
            _ => "local",
        };
        return $"{env}_{entityName}";
    }
}
