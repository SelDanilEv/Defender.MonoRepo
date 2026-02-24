using Defender.DistributedCache;
using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres;
using Defender.DistributedCache.Postgres.TTL;

namespace Defender.DistributedCache.Tests;

public class ArchitectureTests
{
    [Fact]
    public void LibraryAssembly_ShouldMatchExpectedName()
    {
        var assemblyName = typeof(IDistributedCache).Assembly.GetName().Name;

        Assert.Equal("Defender.DistributedCache", assemblyName);
    }

    [Fact]
    public void KnownTypes_ShouldBePublicAndInDistributedCacheNamespace()
    {
        var knownTypes =
            new[]
            {
                typeof(IDistributedCache),
                typeof(DistributedCacheOptions),
                typeof(PostgresDistributedCache),
                typeof(PostgresCacheCleanupService),
            };

        Assert.All(knownTypes, type =>
        {
            Assert.True(type.IsPublic, $"{type.FullName} should be public.");
            Assert.NotNull(type.Namespace);
            Assert.StartsWith("Defender.DistributedCache", type.Namespace!);
        });
    }
}
