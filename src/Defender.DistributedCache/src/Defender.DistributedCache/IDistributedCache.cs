using System.Linq.Expressions;

namespace Defender.DistributedCache;

public interface IDistributedCache
{
    Task Add<T>(
        Func<T, string> idProvider,
        T value,
        TimeSpan? ttl = null);

    Task<T?> Get<T>(
        string key,
        Func<Task<T>>? fetchValue = null,
        TimeSpan? ttl = null);

    Task<T?> Get<T>(
        List<Expression<Func<T, bool>>> expressions,
        Func<T, string>? idProvider = null,
        Func<Task<T>>? fetchValue = null,
        TimeSpan? ttl = null);

    Task Invalidate(string key);

    Task Invalidate<T>(List<Expression<Func<T, bool>>> expressions);
}