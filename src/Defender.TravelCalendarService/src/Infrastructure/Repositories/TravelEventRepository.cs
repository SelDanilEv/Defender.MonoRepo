using Defender.Common.Configuration.Options;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.DB.Repositories;
using Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;
using Defender.TravelCalendarService.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.TravelCalendarService.Infrastructure.Repositories;

public class TravelEventRepository : BaseMongoRepository<TravelEvent>, ITravelEventRepository
{
    private readonly Lazy<Task> indexes;

    public TravelEventRepository(IOptions<MongoDbOptions> options)
        : base(options.Value, "TravelEvents")
    {
        indexes = new Lazy<Task>(CreateIndexesAsync);
    }

    public async Task<IReadOnlyList<TravelEvent>> GetVisibleAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await indexes.Value;
            var filter = Builders<TravelEvent>.Filter.Eq(item => item.OwnerUserId, userId)
                | Builders<TravelEvent>.Filter.ElemMatch(item => item.Participants, participant => participant.UserId == userId);
            return await _mongoCollection.Find(filter).ToListAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task<IReadOnlyList<TravelEvent>> GetOwnedAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await indexes.Value;
            return await _mongoCollection.Find(Builders<TravelEvent>.Filter.Eq(item => item.OwnerUserId, userId)).ToListAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task<TravelEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            await indexes.Value;
            return await _mongoCollection.Find(item => item.Id == eventId).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task AddAsync(TravelEvent travelEvent, CancellationToken cancellationToken)
    {
        try
        {
            await indexes.Value;
            await _mongoCollection.InsertOneAsync(travelEvent, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task AddRangeAsync(IEnumerable<TravelEvent> travelEvents, CancellationToken cancellationToken)
    {
        var events = travelEvents.ToArray();
        if (events.Length == 0)
        {
            return;
        }

        try
        {
            await indexes.Value;
            await _mongoCollection.InsertManyAsync(events, cancellationToken: cancellationToken);
        }
        catch (MongoBulkWriteException exception) when (exception.WriteErrors.All(error => error.Category == ServerErrorCategory.DuplicateKey))
        {
        }
        catch (Exception exception)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task<bool> ReplaceAsync(TravelEvent travelEvent, long expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<TravelEvent>.Filter.Eq(item => item.Id, travelEvent.Id)
                & Builders<TravelEvent>.Filter.Eq(item => item.Version, expectedVersion);
            var result = await _mongoCollection.ReplaceOneAsync(filter, travelEvent, new ReplaceOptions { IsUpsert = false }, cancellationToken);
            return result.ModifiedCount == 1;
        }
        catch (Exception exception)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task<bool> DeleteAsync(Guid eventId, long expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<TravelEvent>.Filter.Eq(item => item.Id, eventId)
                & Builders<TravelEvent>.Filter.Eq(item => item.Version, expectedVersion);
            var result = await _mongoCollection.DeleteOneAsync(filter, cancellationToken);
            return result.DeletedCount == 1;
        }
        catch (Exception exception)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    private Task CreateIndexesAsync()
        => _mongoCollection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<TravelEvent>(Builders<TravelEvent>.IndexKeys.Ascending(item => item.OwnerUserId), new CreateIndexOptions { Name = "ix_owner_user_id" }),
            new CreateIndexModel<TravelEvent>(Builders<TravelEvent>.IndexKeys.Ascending("Participants.UserId"), new CreateIndexOptions { Name = "ix_participants_user_id" }),
        ]);
}
