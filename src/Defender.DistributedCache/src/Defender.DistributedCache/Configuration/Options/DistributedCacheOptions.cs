namespace Defender.DistributedCache.Configuration.Options;

public class DistributedCacheOptions
{
    public string ConnectionString { get; set; } = String.Empty;
    public string CacheTableName { get; set; } = "common_distributed_cache";
    public int TtlForCacheEntriesSeconds { get; set; } = 60 * 30;
}
