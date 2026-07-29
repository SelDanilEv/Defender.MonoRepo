using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres.TTL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Defender.DistributedCache.Postgres.Extensions;

public static class DistributedCacheExtensions
{
    public static IServiceCollection AddPostgresDistributedCache(
        this IServiceCollection services,
        Action<DistributedCacheOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);
        services.AddSingleton<IPostgresCacheCleanupService, PostgresCacheCleanupService>();
        services.AddSingleton<IDistributedCache, PostgresDistributedCache>();
        return services;
    }

    private sealed class PostgresHealthCheck(IOptions<DistributedCacheOptions> options) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
            {
                return HealthCheckResult.Unhealthy("PostgreSQL connection string is not configured.");
            }

            try
            {
                await using var connection = new NpgsqlConnection(options.Value.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy();
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.", exception);
            }
        }
    }
}
