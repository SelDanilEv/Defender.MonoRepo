using Defender.Common.Wrapper;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;

namespace Defender.Portal.Infrastructure.Clients.PersonalFoodAdviser;

public class PersonalFoodAdviserWrapper(IPersonalFoodAdviserClient client) : BaseSwaggerWrapper, IPersonalFoodAdviserWrapper
{
    public Task<PortalPreferencesDto?> GetPreferencesAsync(CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.GetPreferencesAsync(cancellationToken));

    public Task<PortalPreferencesDto> UpdatePreferencesAsync(IReadOnlyList<string> likes, IReadOnlyList<string> dislikes, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.UpdatePreferencesAsync(likes, dislikes, cancellationToken));

    public Task<PortalMenuSessionDto> CreateSessionAsync(CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.CreateSessionAsync(cancellationToken));

    public Task<PortalMenuSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.GetSessionAsync(sessionId, cancellationToken));

    public Task<IReadOnlyList<string>> UploadSessionImagesAsync(Guid sessionId, Stream[] fileStreams, string[] contentTypes, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.UploadSessionImagesAsync(sessionId, fileStreams, contentTypes, cancellationToken));

    public Task<PortalMenuSessionDto?> ConfirmMenuAsync(Guid sessionId, IReadOnlyList<string> confirmedItems, bool trySomethingNew, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.ConfirmMenuAsync(sessionId, confirmedItems, trySomethingNew, cancellationToken));

    public Task RequestParsingAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.RequestParsingAsync(sessionId, cancellationToken));

    public Task RequestRecommendationsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.RequestRecommendationsAsync(sessionId, cancellationToken));

    public Task<IReadOnlyList<string>?> GetRecommendationsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.GetRecommendationsAsync(sessionId, cancellationToken));

    public Task SubmitRatingAsync(string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.SubmitRatingAsync(dishName, rating, sessionId, cancellationToken));
}
