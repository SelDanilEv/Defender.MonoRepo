using Defender.Common.Configuration.Options;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.PersonalFoodAdviser.Infrastructure.Repositories;

public class RecommendationsOutboxRepository : IRecommendationsOutboxRepository
{
    private readonly IMongoCollection<RecommendationsOutboxMessage> _collection;

    public RecommendationsOutboxRepository(IOptions<MongoDbOptions> mongoOption)
    {
        var client = new MongoClient(mongoOption.Value.ConnectionString);
        var database = client.GetDatabase(mongoOption.Value.GetDatabaseName());
        _collection = database.GetCollection<RecommendationsOutboxMessage>("RecommendationsOutboxMessage");
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var sessionIdIndex = new CreateIndexModel<RecommendationsOutboxMessage>(
            Builders<RecommendationsOutboxMessage>.IndexKeys.Ascending(x => x.SessionId),
            new CreateIndexOptions { Unique = true, Name = "ux_recommendations_outbox_session_id" });

        var dueMessageIndex = new CreateIndexModel<RecommendationsOutboxMessage>(
            Builders<RecommendationsOutboxMessage>.IndexKeys
                .Ascending(x => x.NextAttemptAtUtc)
                .Ascending(x => x.LockedUntilUtc)
                .Ascending(x => x.CreatedAtUtc),
            new CreateIndexOptions { Name = "ix_recommendations_outbox_due_messages" });

        _collection.Indexes.CreateMany(new[] { sessionIdIndex, dueMessageIndex });
    }

    public async Task EnqueueAsync(RecommendationsOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var update = Builders<RecommendationsOutboxMessage>.Update
            .SetOnInsert(x => x.Id, message.Id == Guid.Empty ? Guid.NewGuid() : message.Id)
            .SetOnInsert(x => x.CreatedAtUtc, nowUtc)
            .Set(x => x.SessionId, message.SessionId)
            .Set(x => x.UserId, message.UserId)
            .Set(x => x.ConfirmedItems, message.ConfirmedItems)
            .Set(x => x.TrySomethingNew, message.TrySomethingNew)
            .Set(x => x.Attempt, message.Attempt)
            .Set(x => x.NextAttemptAtUtc, message.NextAttemptAtUtc)
            .Set(x => x.UpdatedAtUtc, nowUtc)
            .Set(x => x.LockedUntilUtc, null)
            .Set(x => x.HandlerId, null)
            .Set(x => x.LastError, message.LastError);

        await _collection.UpdateOneAsync(
            Builders<RecommendationsOutboxMessage>.Filter.Eq(x => x.SessionId, message.SessionId),
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<RecommendationsOutboxMessage?> ClaimNextDueAsync(
        Guid handlerId,
        DateTime nowUtc,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<RecommendationsOutboxMessage>.Filter.And(
            Builders<RecommendationsOutboxMessage>.Filter.Lte(x => x.NextAttemptAtUtc, nowUtc),
            Builders<RecommendationsOutboxMessage>.Filter.Or(
                Builders<RecommendationsOutboxMessage>.Filter.Eq(x => x.LockedUntilUtc, null),
                Builders<RecommendationsOutboxMessage>.Filter.Lte(x => x.LockedUntilUtc, nowUtc)));

        var update = Builders<RecommendationsOutboxMessage>.Update
            .Set(x => x.HandlerId, handlerId)
            .Set(x => x.LockedUntilUtc, nowUtc.Add(lockDuration))
            .Set(x => x.UpdatedAtUtc, nowUtc);

        var options = new FindOneAndUpdateOptions<RecommendationsOutboxMessage>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = Builders<RecommendationsOutboxMessage>.Sort
                .Ascending(x => x.NextAttemptAtUtc)
                .Ascending(x => x.CreatedAtUtc)
        };

        return await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }

    public async Task CompleteAsync(Guid id, Guid handlerId, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(
            Builders<RecommendationsOutboxMessage>.Filter.And(
                Builders<RecommendationsOutboxMessage>.Filter.Eq(x => x.Id, id),
                Builders<RecommendationsOutboxMessage>.Filter.Eq(x => x.HandlerId, handlerId)),
            cancellationToken);
    }

    public async Task ReleaseAsync(
        Guid id,
        Guid handlerId,
        DateTime nextAttemptAtUtc,
        string? lastError,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<RecommendationsOutboxMessage>.Update
            .Set(x => x.NextAttemptAtUtc, nextAttemptAtUtc)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow)
            .Set(x => x.LockedUntilUtc, null)
            .Set(x => x.HandlerId, null)
            .Set(x => x.LastError, lastError);

        await _collection.UpdateOneAsync(
            Builders<RecommendationsOutboxMessage>.Filter.And(
                Builders<RecommendationsOutboxMessage>.Filter.Eq(x => x.Id, id),
                Builders<RecommendationsOutboxMessage>.Filter.Eq(x => x.HandlerId, handlerId)),
            update,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteManyAsync(
            Builders<RecommendationsOutboxMessage>.Filter.Eq(x => x.SessionId, sessionId),
            cancellationToken);
    }
}
