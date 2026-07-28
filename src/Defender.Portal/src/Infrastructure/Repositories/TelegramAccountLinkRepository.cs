using Defender.Common.Configuration.Options;
using Defender.Common.Entities;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Modules.Telegram;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Defender.Portal.Infrastructure.Repositories;

public sealed class TelegramAccountLinkRepository : ITelegramAccountLinkRepository
{
    private readonly IMongoCollection<TelegramAccountLinkDocument> collection;
    private readonly Lazy<Task> indexes;

    public TelegramAccountLinkRepository(IOptions<MongoDbOptions> options)
    {
        var database = new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.GetDatabaseName());
        collection = database.GetCollection<TelegramAccountLinkDocument>("TelegramAccountLinks");
        indexes = new Lazy<Task>(CreateIndexesAsync);
    }

    public async Task<TelegramAccountLink?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await indexes.Value;
        var document = await collection.Find(item => item.AccountId == accountId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : document.ToModel();
    }

    public async Task<TelegramAccountLink?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        await indexes.Value;
        var document = await collection.Find(item => item.TelegramUserId == telegramUserId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : document.ToModel();
    }

    public async Task CreateAsync(TelegramAccountLink link, CancellationToken cancellationToken)
    {
        await indexes.Value;
        try
        {
            await collection.InsertOneAsync(new TelegramAccountLinkDocument(link), cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new TelegramAccountLinkConflictException();
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task DeleteByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await indexes.Value;
        try
        {
            await collection.DeleteOneAsync(item => item.AccountId == accountId, cancellationToken);
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    private Task CreateIndexesAsync() => Task.WhenAll(
        collection.Indexes.CreateOneAsync(new CreateIndexModel<TelegramAccountLinkDocument>(
            Builders<TelegramAccountLinkDocument>.IndexKeys.Ascending(item => item.AccountId),
            new CreateIndexOptions { Name = "ux_account_id", Unique = true })),
        collection.Indexes.CreateOneAsync(new CreateIndexModel<TelegramAccountLinkDocument>(
            Builders<TelegramAccountLinkDocument>.IndexKeys.Ascending(item => item.TelegramUserId),
            new CreateIndexOptions { Name = "ux_telegram_user_id", Unique = true })));

    [BsonIgnoreExtraElements]
    private sealed class TelegramAccountLinkDocument : IBaseModel
    {
        public TelegramAccountLinkDocument()
        {
        }

        public TelegramAccountLinkDocument(TelegramAccountLink source)
        {
            Id = Guid.NewGuid();
            AccountId = source.AccountId;
            TelegramUserId = source.TelegramUserId;
            LinkedAt = source.LinkedAt;
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid AccountId { get; set; }

        public long TelegramUserId { get; set; }

        public DateTimeOffset LinkedAt { get; set; }

        public TelegramAccountLink ToModel() => new(AccountId, TelegramUserId, LinkedAt);
    }
}
