using Defender.Common.Configuration.Options;
using Defender.Common.Entities;
using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Modules.Telegram;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Defender.Portal.Infrastructure.Repositories;

public sealed class TelegramWebhookReceiptRepository : ITelegramWebhookReceiptRepository
{
    private readonly IMongoCollection<TelegramWebhookReceiptDocument> collection;
    private readonly Lazy<Task> indexes;

    public TelegramWebhookReceiptRepository(IOptions<MongoDbOptions> options)
    {
        var database = new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.GetDatabaseName());
        collection = database.GetCollection<TelegramWebhookReceiptDocument>("TelegramWebhookReceipts");
        indexes = new Lazy<Task>(CreateIndexesAsync);
    }

    public async Task<bool> TryCreateAsync(TelegramWebhookReceipt receipt, CancellationToken cancellationToken)
    {
        await indexes.Value;
        try
        {
            await collection.InsertOneAsync(new TelegramWebhookReceiptDocument(receipt), cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    private Task CreateIndexesAsync() => collection.Indexes.CreateOneAsync(
        new CreateIndexModel<TelegramWebhookReceiptDocument>(
            Builders<TelegramWebhookReceiptDocument>.IndexKeys.Ascending(item => item.UpdateId),
            new CreateIndexOptions { Name = "ux_update_id", Unique = true }));

    [BsonIgnoreExtraElements]
    private sealed class TelegramWebhookReceiptDocument : IBaseModel
    {
        public TelegramWebhookReceiptDocument()
        {
        }

        public TelegramWebhookReceiptDocument(TelegramWebhookReceipt source)
        {
            Id = Guid.NewGuid();
            UpdateId = source.UpdateId;
            ReceivedAt = source.ReceivedAt;
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        public long UpdateId { get; set; }

        public DateTimeOffset ReceivedAt { get; set; }
    }
}
