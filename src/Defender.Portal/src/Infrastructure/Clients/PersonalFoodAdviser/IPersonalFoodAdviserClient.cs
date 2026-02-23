using Defender.Portal.Application.DTOs.FoodAdviser;

namespace Defender.Portal.Infrastructure.Clients.PersonalFoodAdviser;

public interface IPersonalFoodAdviserClient
{
    Task<PortalPreferencesDto?> GetPreferencesAsync(CancellationToken cancellationToken = default);
    Task<PortalPreferencesDto> UpdatePreferencesAsync(IReadOnlyList<string> likes, IReadOnlyList<string> dislikes, CancellationToken cancellationToken = default);
    Task<PortalMenuSessionDto> CreateSessionAsync(CancellationToken cancellationToken = default);
    Task<PortalMenuSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> UploadSessionImagesAsync(Guid sessionId, Stream[] fileStreams, string[] contentTypes, CancellationToken cancellationToken = default);
    Task<PortalMenuSessionDto?> ConfirmMenuAsync(Guid sessionId, IReadOnlyList<string> confirmedItems, bool trySomethingNew, CancellationToken cancellationToken = default);
    Task RequestParsingAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task RequestRecommendationsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>?> GetRecommendationsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task SubmitRatingAsync(string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default);
}
