using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres.TTL;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.DistributedCache.Postgres.Extensions;

public static class DistributedCacheExtensions
{
    public static IServiceCollection AddPostgresDistributedCache(
        this IServiceCollection services,
        Action<DistributedCacheOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IPostgresCacheCleanupService, PostgresCacheCleanupService>();
        services.AddSingleton<IDistributedCache, PostgresDistributedCache>();
        return services;
    }
}