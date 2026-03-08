using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres;
using Defender.DistributedCache.Postgres.Extensions;
using Defender.DistributedCache.Postgres.TTL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Defender.DistributedCache.Tests;

public class DistributedCacheExtensionsTests
{
    [Fact]
    public void AddPostgresDistributedCache_WhenCalled_RegistersServicesAndOptions()
    {
        var services = new ServiceCollection();
        const string expectedConnectionString = "Host=127.0.0.1;Port=5432;Database=cache_database;Username=postgres;Password=postgres";
        const string expectedTableName = "cache_entries";
        const int expectedTtl = 99;

        services.AddPostgresDistributedCache(options =>
        {
            options.ConnectionString = expectedConnectionString;
            options.CacheTableName = expectedTableName;
            options.TtlForCacheEntriesSeconds = expectedTtl;
        });

        using var provider = services.BuildServiceProvider();
        var distributedCacheDescriptor = services.Single(x => x.ServiceType == typeof(IDistributedCache));
        var cleanupDescriptor = services.Single(x => x.ServiceType == typeof(IPostgresCacheCleanupService));
        var configuredOptions = provider.GetRequiredService<IOptions<DistributedCacheOptions>>().Value;

        Assert.Equal(ServiceLifetime.Singleton, distributedCacheDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, cleanupDescriptor.Lifetime);
        Assert.Equal(typeof(PostgresDistributedCache), distributedCacheDescriptor.ImplementationType);
        Assert.Equal(typeof(PostgresCacheCleanupService), cleanupDescriptor.ImplementationType);
        Assert.Equal(expectedConnectionString, configuredOptions.ConnectionString);
        Assert.Equal(expectedTableName, configuredOptions.CacheTableName);
        Assert.Equal(expectedTtl, configuredOptions.TtlForCacheEntriesSeconds);
    }
}
