using System.Reflection;
using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.BudgetTracker.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using System.Net;

namespace Defender.BudgetTracker.Tests.Infrastructure;

public class RegularExpenseReviewRepositoryTests
{
    [Fact]
    public void UniqueUserMonthIndex_WhenCreated_UsesUniqueCompoundKeys()
    {
        var model = RegularExpenseReviewIndexes.CreateUniqueUserMonthIndex();
        var keys = model.Keys.Render(new RenderArgs<RegularExpenseReview>(
            BsonSerializer.LookupSerializer<RegularExpenseReview>(),
            BsonSerializer.SerializerRegistry,
            new PathRenderArgs(null, false),
            false,
            false,
            false,
            default));

        Assert.True(model.Options.Unique);
        Assert.Equal(RegularExpenseReviewIndexes.UniqueUserMonthIndexName, model.Options.Name);
        Assert.Equal(1, keys[nameof(RegularExpenseReview.UserId)].AsInt32);
        Assert.Equal(1, keys[nameof(RegularExpenseReview.Month)].AsInt32);
    }

    [Fact]
    public async Task UpsertRegularExpenseReviewAsync_WhenDuplicateKeyRaceOccurs_ReplacesPersistedWinner()
    {
        var userId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var losingReview = new RegularExpenseReview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Month = new DateOnly(2026, 8, 27)
        };
        var persistedWinner = new RegularExpenseReview
        {
            Id = winnerId,
            UserId = userId,
            Month = new DateOnly(2026, 8, 1)
        };
        var duplicateKeyError = (WriteError)Activator.CreateInstance(
            typeof(WriteError),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [ServerErrorCategory.DuplicateKey, 11000, "duplicate key", new BsonDocument()],
            null)!;
        var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
        var duplicateKey = new MongoWriteException(new ConnectionId(serverId), duplicateKeyError, null, null);
        var collection = new Mock<IMongoCollection<RegularExpenseReview>>();
        var indexManager = new Mock<IMongoIndexManager<RegularExpenseReview>>();
        var updates = new List<(UpdateDefinition<RegularExpenseReview> Update, bool IsUpsert, ReturnDocument ReturnDocument)>();

        collection.SetupGet(item => item.Indexes).Returns(indexManager.Object);
        indexManager
            .Setup(item => item.CreateOneAsync(
                It.IsAny<CreateIndexModel<RegularExpenseReview>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegularExpenseReviewIndexes.UniqueUserMonthIndexName);
        collection
            .Setup(item => item.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<RegularExpenseReview>>(),
                It.IsAny<UpdateDefinition<RegularExpenseReview>>(),
                It.IsAny<FindOneAndUpdateOptions<RegularExpenseReview>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<RegularExpenseReview> _,
                UpdateDefinition<RegularExpenseReview> update,
                FindOneAndUpdateOptions<RegularExpenseReview> options,
                CancellationToken _) =>
            {
                updates.Add((update, options.IsUpsert, options.ReturnDocument));
                if (updates.Count == 1)
                {
                    throw duplicateKey;
                }

                return Task.FromResult(persistedWinner);
            });

        var repository = new RegularExpenseReviewRepository(Options.Create(new MongoDbOptions
        {
            AppName = "BudgetTrackerTests",
            Environment = "Test",
            ConnectionString = "mongodb://localhost:27017"
        }));
        typeof(BaseMongoRepository<RegularExpenseReview>)
            .GetField("_mongoCollection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(repository, collection.Object);

        var result = await repository.UpsertRegularExpenseReviewAsync(losingReview);

        Assert.Equal(winnerId, result.Id);
        Assert.Equal(2, updates.Count);
        Assert.True(updates[0].IsUpsert);
        Assert.False(updates[1].IsUpsert);
        Assert.Equal(ReturnDocument.After, updates[1].ReturnDocument);
    }

    [Fact]
    public async Task UpsertRegularExpenseReviewAsync_WhenConcurrentWriterInsertsBeforeAtomicUpsert_PreservesExistingIdWithoutImmutableIdFailure()
    {
        var userId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var losingReview = new RegularExpenseReview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Month = new DateOnly(2026, 8, 27)
        };
        var persistedWinner = new RegularExpenseReview
        {
            Id = winnerId,
            UserId = userId,
            Month = new DateOnly(2026, 8, 1)
        };
        var collection = new Mock<IMongoCollection<RegularExpenseReview>>();
        var indexManager = new Mock<IMongoIndexManager<RegularExpenseReview>>();
        var atomicOperations = new List<(
            BsonDocument Filter,
            BsonDocument Update,
            FindOneAndUpdateOptions<RegularExpenseReview> Options)>();
        var persistedReviews = new List<RegularExpenseReview>();

        collection.SetupGet(item => item.Indexes).Returns(indexManager.Object);
        indexManager
            .Setup(item => item.CreateOneAsync(
                It.IsAny<CreateIndexModel<RegularExpenseReview>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegularExpenseReviewIndexes.UniqueUserMonthIndexName);
        collection
            .Setup(item => item.ReplaceOneAsync(
                It.IsAny<FilterDefinition<RegularExpenseReview>>(),
                It.IsAny<RegularExpenseReview>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<RegularExpenseReview> _,
                RegularExpenseReview replacement,
                ReplaceOptions options,
                CancellationToken _) =>
            {
                // Model A winning the insert between B's absent pre-read and replacement.
                if (options.IsUpsert && persistedReviews.Count == 0)
                {
                    persistedReviews.Add(persistedWinner);
                }

                throw new InvalidOperationException(
                    $"Mongo immutable _id violation for {replacement.Id} against {persistedReviews.Single().Id}");
            });
        collection
            .Setup(item => item.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<RegularExpenseReview>>(),
                It.IsAny<UpdateDefinition<RegularExpenseReview>>(),
                It.IsAny<FindOneAndUpdateOptions<RegularExpenseReview>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<RegularExpenseReview> filter,
                UpdateDefinition<RegularExpenseReview> update,
                FindOneAndUpdateOptions<RegularExpenseReview> options,
                CancellationToken _) =>
            {
                // Model A's successful insert before B reaches its atomic upsert.
                if (persistedReviews.Count == 0)
                {
                    persistedReviews.Add(persistedWinner);
                }

                var renderArgs = new RenderArgs<RegularExpenseReview>(
                    BsonSerializer.LookupSerializer<RegularExpenseReview>(),
                    BsonSerializer.SerializerRegistry,
                    new PathRenderArgs(null, false),
                    false,
                    false,
                    false,
                    default);
                atomicOperations.Add((
                    filter.Render(renderArgs),
                    update.Render(renderArgs).AsBsonDocument,
                    options));
                return Task.FromResult(persistedReviews.Single());
            });

        var repository = new RegularExpenseReviewRepository(Options.Create(new MongoDbOptions
        {
            AppName = "BudgetTrackerTests",
            Environment = "Test",
            ConnectionString = "mongodb://localhost:27017"
        }));
        typeof(BaseMongoRepository<RegularExpenseReview>)
            .GetField("_mongoCollection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(repository, collection.Object);

        var result = await repository.UpsertRegularExpenseReviewAsync(losingReview);

        Assert.Equal(winnerId, result.Id);
        Assert.Single(persistedReviews);
        Assert.Equal(winnerId, persistedReviews[0].Id);
        var operation = Assert.Single(atomicOperations);
        Assert.True(operation.Options.IsUpsert);
        Assert.Equal(ReturnDocument.After, operation.Options.ReturnDocument);
        Assert.Equal(2, operation.Filter.ElementCount);
        Assert.True(operation.Filter.Contains(nameof(RegularExpenseReview.UserId)));
        Assert.True(operation.Filter.Contains(nameof(RegularExpenseReview.Month)));
        Assert.False(operation.Filter.Contains(nameof(RegularExpenseReview.Id)));

        var setOnInsert = operation.Update["$setOnInsert"].AsBsonDocument;
        Assert.Equal(BsonType.String, setOnInsert["_id"].BsonType);
        Assert.Equal(losingReview.Id.ToString(), setOnInsert["_id"].AsString);
        Assert.False(operation.Update["$set"].AsBsonDocument.Contains("_id"));
        collection.Verify(item => item.ReplaceOneAsync(
            It.IsAny<FilterDefinition<RegularExpenseReview>>(),
            It.IsAny<RegularExpenseReview>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
