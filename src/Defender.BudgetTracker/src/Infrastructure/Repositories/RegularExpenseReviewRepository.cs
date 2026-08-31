using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.DB.Repositories;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace Defender.BudgetTracker.Infrastructure.Repositories;

public class RegularExpenseReviewRepository
    : BaseMongoRepository<RegularExpenseReview>, IRegularExpenseReviewRepository
{
    private readonly Lazy<Task> indexes;

    public RegularExpenseReviewRepository(IOptions<MongoDbOptions> mongoOption)
        : base(mongoOption.Value, "RegularExpenseReviews")
    {
        indexes = new(CreateIndexesAsync);
    }

    public async Task<PagedResult<RegularExpenseReview>> GetRegularExpenseReviewsAsync(
        PaginationRequest pagination,
        Guid userId)
    {
        await indexes.Value;
        var filter = FindModelRequest<RegularExpenseReview>
            .Init(review => review.UserId, userId)
            .Sort(review => review.Month, SortType.Desc);
        var settings = PaginationSettings<RegularExpenseReview>
            .FromPaginationRequest(pagination)
            .SetupFindOptions(filter);

        return await GetItemsAsync(settings);
    }

    public async Task<List<RegularExpenseReview>> GetRegularExpenseReviewsAsync(
        Guid userId,
        DateOnly startMonth,
        DateOnly endMonth)
    {
        await indexes.Value;
        var filter = FindModelRequest<RegularExpenseReview>
            .Init(review => review.UserId, userId)
            .And(review => review.Month, startMonth, FilterType.Gte)
            .And(review => review.Month, endMonth, FilterType.Lte)
            .Sort(review => review.Month, SortType.Desc);
        var settings = PaginationSettings<RegularExpenseReview>.WithoutPagination()
            .SetupFindOptions(filter);
        var result = await GetItemsAsync(settings);

        return [.. result.Items];
    }

    public async Task<RegularExpenseReview?> GetRegularExpenseReviewAsync(Guid userId, DateOnly month)
    {
        await indexes.Value;
        var filter = FindModelRequest<RegularExpenseReview>
            .Init(review => review.UserId, userId)
            .And(review => review.Month, RegularExpenseReview.NormalizeMonth(month));

        return await GetItemAsync(filter);
    }

    public async Task<RegularExpenseReview?> GetRegularExpenseReviewAsync(Guid userId, Guid reviewId)
    {
        await indexes.Value;
        var filter = FindModelRequest<RegularExpenseReview>
            .Init(review => review.UserId, userId)
            .And(review => review.Id, reviewId);

        return await GetItemAsync(filter);
    }

    public async Task<RegularExpenseReview?> GetLatestRegularExpenseReviewAsync(Guid userId)
    {
        await indexes.Value;
        var filter = FindModelRequest<RegularExpenseReview>
            .Init(review => review.UserId, userId)
            .Sort(review => review.Month, SortType.Desc);

        return await GetItemAsync(filter);
    }

    public async Task<RegularExpenseReview> UpsertRegularExpenseReviewAsync(RegularExpenseReview review)
    {
        await indexes.Value;
        review.Month = RegularExpenseReview.NormalizeMonth(review.Month);
        review.Id = review.Id == Guid.Empty ? Guid.NewGuid() : review.Id;
        var filter = FindModelRequest<RegularExpenseReview>
            .Init(existing => existing.UserId, review.UserId)
            .And(existing => existing.Month, review.Month);
        var update = Builders<RegularExpenseReview>.Update
            .SetOnInsert(existing => existing.Id, review.Id)
            .Set(existing => existing.UserId, review.UserId)
            .Set(existing => existing.Month, review.Month)
            .Set(existing => existing.Expenses, review.Expenses)
            .Set(existing => existing.RatesModel, review.RatesModel);
        var options = new FindOneAndUpdateOptions<RegularExpenseReview>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        try
        {
            return await _mongoCollection.FindOneAndUpdateAsync(
                filter.BuildFilterDefinition(),
                update,
                options) ?? throw new InvalidOperationException(
                    "MongoDB did not return the upserted regular expense review.");
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            options.IsUpsert = false;
            var persistedWinner = await _mongoCollection.FindOneAndUpdateAsync(
                filter.BuildFilterDefinition(),
                update,
                options);

            if (persistedWinner is null)
            {
                throw;
            }

            return persistedWinner;
        }
    }

    public async Task DeleteRegularExpenseReviewAsync(Guid reviewId, Guid userId)
    {
        await indexes.Value;
        var filter = FindModelRequest<RegularExpenseReview>
            .Init(review => review.UserId, userId)
            .And(review => review.Id, reviewId);

        await RemoveItemAsync(reviewId, filter);
    }

    private Task CreateIndexesAsync()
        => _mongoCollection.Indexes.CreateOneAsync(
            RegularExpenseReviewIndexes.CreateUniqueUserMonthIndex());
}
