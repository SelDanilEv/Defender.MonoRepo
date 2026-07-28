using Defender.Common.Configuration.Options;
using Defender.Common.Entities;
using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Modules.Telegram;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Defender.Portal.Infrastructure.Repositories;

public sealed class TelegramLinkHandoffRepository : ITelegramLinkHandoffRepository
{
    private readonly IMongoCollection<TelegramLinkHandoffDocument> collection;
    private readonly Lazy<Task> indexes;

    public TelegramLinkHandoffRepository(IOptions<MongoDbOptions> options)
    {
        var database = new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.GetDatabaseName());
        collection = database.GetCollection<TelegramLinkHandoffDocument>("TelegramLinkHandoffs");
        indexes = new Lazy<Task>(CreateIndexesAsync);
    }

    public async Task CreateAsync(string codeHash, long telegramUserId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await indexes.Value;
        await collection.InsertOneAsync(new TelegramLinkHandoffDocument(codeHash, telegramUserId, expiresAt), cancellationToken: cancellationToken);
    }

    public async Task<long?> TryConsumeAsync(string codeHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await indexes.Value;
        var document = await collection.FindOneAndDeleteAsync(
            item => item.CodeHash == codeHash && item.ExpiresAt > now,
            cancellationToken: cancellationToken);
        return document?.TelegramUserId;
    }

    private Task CreateIndexesAsync() => Task.WhenAll(
        collection.Indexes.CreateOneAsync(new CreateIndexModel<TelegramLinkHandoffDocument>(
            Builders<TelegramLinkHandoffDocument>.IndexKeys.Ascending(item => item.CodeHash),
            new CreateIndexOptions { Name = "ux_code_hash", Unique = true })),
        collection.Indexes.CreateOneAsync(new CreateIndexModel<TelegramLinkHandoffDocument>(
            Builders<TelegramLinkHandoffDocument>.IndexKeys.Ascending(item => item.ExpiresAt),
            new CreateIndexOptions { Name = "ttl_expires", ExpireAfter = TimeSpan.Zero })));

    [BsonIgnoreExtraElements]
    private sealed class TelegramLinkHandoffDocument : IBaseModel
    {
        public TelegramLinkHandoffDocument()
        {
        }

        public TelegramLinkHandoffDocument(string codeHash, long telegramUserId, DateTimeOffset expiresAt)
        {
            Id = Guid.NewGuid();
            CodeHash = codeHash;
            TelegramUserId = telegramUserId;
            ExpiresAt = expiresAt;
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        public string CodeHash { get; set; } = string.Empty;
        public long TelegramUserId { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
