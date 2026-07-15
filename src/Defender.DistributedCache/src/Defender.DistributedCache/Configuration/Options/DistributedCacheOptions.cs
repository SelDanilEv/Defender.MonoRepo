namespace Defender.DistributedCache.Configuration.Options;

public class DistributedCacheOptions
{
    public string ConnectionString { get; set; } = String.Empty;
    public string CacheTableName { get; set; } = "common_distributed_cache";
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(30);

    // Legacy configuration key. New deployments should use DefaultTtl.
    public int? TtlForCacheEntriesSeconds { get; set; }

    public TimeSpan ResolveDefaultTtl()
    {
        return TtlForCacheEntriesSeconds is > 0
            ? TimeSpan.FromSeconds(TtlForCacheEntriesSeconds.Value)
            : DefaultTtl;
    }
}
