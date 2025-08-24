using Dapper;
using Defender.DistributedCache.Configuration.Options;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Defender.DistributedCache.Postgres
{
    public class PostgresDistributedCache : IDistributedCache
    {
        private readonly DistributedCacheOptions _options;
        private readonly ILogger<PostgresDistributedCache> _logger;

        private bool IsConnectionEstablished { get; set; } = false;

        private const string ConnectionFailedMessage =
            "Connection to the cache database was not established. Default actions will be performed.";

        public PostgresDistributedCache(
            IOptions<DistributedCacheOptions> options,
            ILogger<PostgresDistributedCache> logger
        )
        {
            _options = options.Value;
            _logger = logger;

            Task.Run(Init);
        }

        private async Task Init()
        {
            do
            {
                try
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var createTableQuery = $@"
                        CREATE TABLE IF NOT EXISTS {_options.CacheTableName} (
                            key TEXT PRIMARY KEY,
                            value JSONB NOT NULL,
                            expiration TIMESTAMP NOT NULL
                        );";

                    await connection.ExecuteAsync(createTableQuery);

                    IsConnectionEstablished = true;
                }
                catch (Exception exception)
                {
                    IsConnectionEstablished = false;
                    _logger.LogError(exception, ConnectionFailedMessage);
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            } while (!CheckIfConnectionEstablished());
        }

        private bool CheckIfConnectionEstablished()
        {
            if (!IsConnectionEstablished)
            {
                _logger.LogWarning(ConnectionFailedMessage);
            }

            return IsConnectionEstablished;
        }

        public async Task Add<T>(
            Func<T, string> idProvider,
            T value,
            TimeSpan? ttl = null)
        {
            if (!CheckIfConnectionEstablished()) return;

            ttl ??= TimeSpan.FromMinutes(_options.TtlForCacheEntriesSeconds);

            var json = JsonSerializer.Serialize(value);
            var expiration = DateTime.UtcNow.Add(ttl.Value);

            var query = $@"
                INSERT INTO {_options.CacheTableName} (key, value, expiration)
                VALUES (@Key, @Value::jsonb, @Expiration)
                ON CONFLICT (key)
                DO UPDATE SET value = @Value::jsonb, expiration = @Expiration";

            await ExecuteNonQueryAsync(query,
                new
                {
                    Key = idProvider(value),
                    Value = json,
                    Expiration = expiration
                });
        }

        public async Task<T?> Get<T>(
            string key,
            Func<Task<T>>? fetchValue = null,
            TimeSpan? ttl = null)
        {
            if (!CheckIfConnectionEstablished())
                return fetchValue is null ? default : await fetchValue();

            var query = $"SELECT value FROM {_options.CacheTableName} WHERE key = @Key";
            var result = await ExecuteQueryAsync<string>(query, new { Key = key });

            if (result != null)
            {
                return JsonSerializer.Deserialize<T>(result);
            }

            if (fetchValue is null) return default;

            var value = await fetchValue();
            if (value is not null)
            {
                await Add(_ => key, value, ttl);
            }

            return value;
        }

        public async Task<T?> Get<T>(
            List<Expression<Func<T, bool>>> expressions,
            Func<T, string>? idProvider = null,
            Func<Task<T>>? fetchValue = null,
            TimeSpan? ttl = null)
        {
            if (!CheckIfConnectionEstablished())
                return fetchValue is null ? default : await fetchValue();

            var (conditions, parameters) = ParseExpressions(expressions);

            var query = $@"
                SELECT value
                FROM {_options.CacheTableName}
                WHERE {string.Join(" AND ", conditions)}";

            var result = await ExecuteQueryAsync<string>(query, parameters);

            if (result != null)
            {
                return JsonSerializer.Deserialize<T>(result);
            }

            if (idProvider is null || fetchValue is null) return default;

            var value = await fetchValue();
            if (value is not null)
            {
                await Add(idProvider, value, ttl);
            }

            return value;
        }

        public async Task Invalidate(string key)
        {
            if (!CheckIfConnectionEstablished()) return;

            var query = $"DELETE FROM {_options.CacheTableName} WHERE key = @Key";
            await ExecuteNonQueryAsync(query, new { Key = key });
        }

        public async Task Invalidate<T>(List<Expression<Func<T, bool>>> expressions)
        {
            if (!CheckIfConnectionEstablished()) return;

            var (conditions, parameters) = ParseExpressions(expressions);

            var query = $@"
                DELETE FROM {_options.CacheTableName}
                WHERE {string.Join(" AND ", conditions)}";

            await ExecuteNonQueryAsync(query, parameters);
        }

        #region Private methods

        private NpgsqlConnection CreateConnection() => new(_options.ConnectionString);

        private async Task<T?> ExecuteQueryAsync<T>(string query, object parameters)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<T>(query, parameters);
            }
            catch (Exception)
            {
                return default;
            }
        }

        private async Task ExecuteNonQueryAsync(string query, object parameters)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                await connection.ExecuteAsync(query, parameters);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static (List<string>, DynamicParameters) ParseExpressions<T>(
            List<Expression<Func<T, bool>>> expressions)
        {
            var parameters = new DynamicParameters();

            var conditions = expressions.ConvertAll(expression => ExpressionToSql(expression, parameters));

            return (conditions, parameters);
        }

        private static string ExpressionToSql<T>(
            Expression<Func<T, bool>> expression,
            DynamicParameters parameters)
        {
            if (expression.Body is BinaryExpression binaryExpression)
            {
                var left = GetMemberExpression(binaryExpression.Left);
                var right = GetExpressionValue(binaryExpression.Right);

                if (left != null && right != null)
                {
                    var paramName = $"@{left.Member.Name}";
                    parameters.Add(paramName, right);
                    return $"value @> jsonb_build_object('{left.Member.Name}', {paramName})";
                }
            }

            throw new NotSupportedException("Only simple binary expressions are supported.");
        }

        private static MemberExpression? GetMemberExpression(Expression expression)
        {
            return expression switch
            {
                MemberExpression memberExpression => memberExpression,
                UnaryExpression { Operand: MemberExpression operand } => operand,
                _ => null
            };
        }

        private static object? GetExpressionValue(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constantExpression:
                    return constantExpression.Value;
                case MemberExpression memberExpression:
                    {
                        var objectMember = Expression.Convert(memberExpression, typeof(object));
                        var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                        var getter = getterLambda.Compile();
                        return getter();
                    }
                case MethodCallExpression methodCallExpression:
                    {
                        var methodCall = Expression.Lambda(methodCallExpression).Compile();
                        return methodCall.DynamicInvoke();
                    }
                default:
                    throw new NotSupportedException("Unsupported expression type.");
            }
        }

        #endregion Private methods
    }
}