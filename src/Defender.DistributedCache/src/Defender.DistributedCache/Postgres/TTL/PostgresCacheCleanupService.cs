using Dapper;
using Defender.DistributedCache.Configuration.Options;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace Defender.DistributedCache.Postgres.TTL;

public class PostgresCacheCleanupService : IPostgresCacheCleanupService
{
    private readonly DistributedCacheOptions _options;

    private const string JobName = "delete_expired_cache_entries";

    public PostgresCacheCleanupService(IOptions<DistributedCacheOptions> options)
    {
        _options = options.Value;

        Init();
    }

    private void Init()
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();

            var createCleanupProcedureQuery = $@"
                CREATE OR REPLACE FUNCTION {JobName}() RETURNS void AS $$
                BEGIN
                    DELETE FROM {_options.CacheTableName} WHERE expiration < NOW();
                END;
                $$ LANGUAGE plpgsql;";

            _ = connection.ExecuteAsync(createCleanupProcedureQuery);
        }
        catch
        {
            // ignored
        }
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_options.ConnectionString);
    }

    public async Task CheckAndRunCleanupAsync()
    {
        using var connection = CreateConnection();
        connection.Open();

        await connection.ExecuteAsync("SELECT delete_expired_cache_entries();");
    }
}