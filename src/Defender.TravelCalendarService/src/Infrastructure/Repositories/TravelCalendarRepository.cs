using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;
using Defender.TravelCalendarService.Application.Defaults;
using Defender.TravelCalendarService.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.TravelCalendarService.Infrastructure.Repositories;

public class TravelCalendarRepository : BaseMongoRepository<TravelCalendar>, ITravelCalendarRepository
{
    private readonly TravelCalendarDefaultsFactory defaultsFactory;
    private readonly TimeProvider timeProvider;
    private readonly Lazy<Task> indexes;

    public TravelCalendarRepository(IOptions<MongoDbOptions> options, TravelCalendarDefaultsFactory defaultsFactory, TimeProvider timeProvider)
        : base(options.Value, "TravelCalendars")
    {
        this.defaultsFactory = defaultsFactory;
        this.timeProvider = timeProvider;
        indexes = new(CreateIndexesAsync);
    }

    public async Task<TravelCalendar> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await indexes.Value;
            var filter = Builders<TravelCalendar>.Filter.Eq(item => item.UserId, userId);
            var existing = await _mongoCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (existing != null) return existing;
            var created = defaultsFactory.Create(userId, timeProvider.GetUtcNow());
            try { await _mongoCollection.InsertOneAsync(created, cancellationToken: cancellationToken); return created; }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return await _mongoCollection.Find(filter).FirstAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task<bool> ReplaceAsync(TravelCalendar calendar, long expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<TravelCalendar>.Filter.Eq(item => item.Id, calendar.Id)
                & Builders<TravelCalendar>.Filter.Eq(item => item.UserId, calendar.UserId)
                & Builders<TravelCalendar>.Filter.Eq(item => item.Version, expectedVersion);
            var result = await _mongoCollection.ReplaceOneAsync(filter, calendar, new ReplaceOptions { IsUpsert = false }, cancellationToken);
            return result.ModifiedCount == 1;
        }
        catch (Exception exception) { throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception); }
    }

    private Task CreateIndexesAsync() => _mongoCollection.Indexes.CreateOneAsync(new CreateIndexModel<TravelCalendar>(
        Builders<TravelCalendar>.IndexKeys.Ascending(item => item.UserId), new CreateIndexOptions { Name = "ux_user_id", Unique = true }));
}
