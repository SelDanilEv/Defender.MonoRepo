using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IMenuSessionService
{
    Task<MenuSession> CreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MenuSession?> GetByIdAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
    Task<MenuSession?> UpdateImageRefsAsync(Guid sessionId, Guid userId, IReadOnlyList<string> imageRefs, CancellationToken cancellationToken = default);
    Task<MenuSession?> ConfirmAsync(Guid sessionId, Guid userId, IReadOnlyList<string> confirmedItems, bool trySomethingNew, CancellationToken cancellationToken = default);
    Task RequestParsingAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
    Task RequestRecommendationsAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>?> GetRecommendationsAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
}
