namespace Defender.DistributedCache;

public interface IPostgresCacheCleanupService
{
    Task CheckAndRunCleanupAsync();
}