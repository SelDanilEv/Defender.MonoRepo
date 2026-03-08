using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres.TTL;
using Microsoft.Extensions.Options;

namespace Defender.DistributedCache.Tests;

public class PostgresCacheCleanupServiceTests
{
    [Fact]
    public void Constructor_WhenConnectionIsInvalid_DoesNotThrow()
    {
        var options = CreateOptions();

        var exception = Record.Exception(() => _ = new PostgresCacheCleanupService(options));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CheckAndRunCleanupAsync_WhenConnectionIsInvalid_ThrowsException()
    {
        var sut = new PostgresCacheCleanupService(CreateOptions());

        await Assert.ThrowsAnyAsync<Exception>(() => sut.CheckAndRunCleanupAsync());
    }

    private static IOptions<DistributedCacheOptions> CreateOptions()
    {
        return Options.Create(new DistributedCacheOptions
        {
            ConnectionString = "Host=127.0.0.1;Port=1;Database=cache_database;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1;Pooling=false",
            CacheTableName = "cache"
        });
    }
}
