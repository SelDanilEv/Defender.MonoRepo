namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IMenuIntelligenceClient
{
    Task<IReadOnlyList<string>> ExtractDishNamesFromImagesAsync(IReadOnlyList<byte[]> imageBytes, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRankedRecommendationsAsync(
        IReadOnlyList<string> confirmedDishes,
        IReadOnlyList<string> likes,
        IReadOnlyList<string> dislikes,
        IReadOnlyList<(string DishName, int Rating)> ratingHistory,
        bool trySomethingNew,
        int topN,
        CancellationToken cancellationToken = default);
}
