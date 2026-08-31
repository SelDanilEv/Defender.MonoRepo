using Defender.BudgetTracker.Domain.Entities.Reviews;
using MongoDB.Driver;

namespace Defender.BudgetTracker.Infrastructure.Repositories;

internal static class RegularExpenseReviewIndexes
{
    public const string UniqueUserMonthIndexName = "ux_regular_expense_review_user_month";

    public static CreateIndexModel<RegularExpenseReview> CreateUniqueUserMonthIndex()
    {
        var keys = Builders<RegularExpenseReview>.IndexKeys.Combine(
            Builders<RegularExpenseReview>.IndexKeys.Ascending(review => review.UserId),
            Builders<RegularExpenseReview>.IndexKeys.Ascending(review => review.Month));

        return new CreateIndexModel<RegularExpenseReview>(
            keys,
            new CreateIndexOptions
            {
                Name = UniqueUserMonthIndexName,
                Unique = true
            });
    }
}
