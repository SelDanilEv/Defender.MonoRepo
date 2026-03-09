using Defender.Common.Configuration.Options;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.PersonalFoodAdvisor.Infrastructure.Repositories;

public class GeminiModelLoopStateRepository : IGeminiModelLoopStateRepository
{
    private readonly IMongoCollection<GeminiModelLoopState> _collection;

    public GeminiModelLoopStateRepository(IOptions<MongoDbOptions> mongoOption)
    {
        var client = new MongoClient(mongoOption.Value.ConnectionString);
        var database = client.GetDatabase(mongoOption.Value.GetDatabaseName());
        _collection = database.GetCollection<GeminiModelLoopState>("GeminiModelLoopState");
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var routeIndex = new CreateIndexModel<GeminiModelLoopState>(
            Builders<GeminiModelLoopState>.IndexKeys.Ascending(x => x.Route),
            new CreateIndexOptions { Unique = true, Name = "ux_gemini_model_loop_route" });

        _collection.Indexes.CreateOne(routeIndex);
    }

    public async Task<IReadOnlyList<GeminiModelLoopState>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(Builders<GeminiModelLoopState>.Filter.Empty)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(GeminiModelLoopState state, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var routeId = state.Route.ToString();

        var update = Builders<GeminiModelLoopState>.Update
            .SetOnInsert(x => x.Id, routeId)
            .SetOnInsert(x => x.CreatedAtUtc, state.CreatedAtUtc == default ? nowUtc : state.CreatedAtUtc)
            .Set(x => x.Route, state.Route)
            .Set(x => x.Models, state.Models)
            .Set(x => x.ActiveModelIndex, state.ActiveModelIndex)
            .Set(x => x.LastSwitchAtUtc, state.LastSwitchAtUtc)
            .Set(x => x.LastResetDateUtc, state.LastResetDateUtc)
            .Set(x => x.UpdatedAtUtc, nowUtc);

        await _collection.UpdateOneAsync(
            Builders<GeminiModelLoopState>.Filter.Eq(x => x.Route, state.Route),
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }
}
