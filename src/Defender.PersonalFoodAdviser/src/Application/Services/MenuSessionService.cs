using Defender.Kafka.Default;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Defender.PersonalFoodAdviser.Domain.Enums;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class MenuSessionService(
    IMenuSessionRepository repository,
    IDefaultKafkaProducer<MenuParsingRequestedEvent> menuParsingProducer,
    IDefaultKafkaProducer<RecommendationsRequestedEvent> recommendationsProducer) : IMenuSessionService
{
    public async Task<MenuSession> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var session = new MenuSession
        {
            UserId = userId,
            Status = MenuSessionStatus.Uploaded
        };
        return await repository.CreateAsync(session, cancellationToken);
    }

    public Task<MenuSession?> GetByIdAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
        => GetOwnedSessionAsync(sessionId, userId, cancellationToken);

    public async Task<MenuSession?> UpdateImageRefsAsync(Guid sessionId, Guid userId, IReadOnlyList<string> imageRefs, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null) return null;
        session.ImageRefs = imageRefs?.ToList() ?? [];
        return await repository.UpdateAsync(session, cancellationToken);
    }

    public async Task<MenuSession?> ConfirmAsync(Guid sessionId, Guid userId, IReadOnlyList<string> confirmedItems, bool trySomethingNew, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null) return null;
        session.ConfirmedItems = confirmedItems?.ToList() ?? [];
        session.TrySomethingNew = trySomethingNew;
        session.Status = MenuSessionStatus.Confirmed;
        return await repository.UpdateAsync(session, cancellationToken);
    }

    public async Task RequestParsingAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null) return;
        var evt = new MenuParsingRequestedEvent(session.Id, session.UserId, session.ImageRefs);
        await menuParsingProducer.ProduceAsync(KafkaTopicNames.MenuParsingRequested, evt, cancellationToken);
    }

    public async Task RequestRecommendationsAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null) return;
        var evt = new RecommendationsRequestedEvent(
            session.Id,
            session.UserId,
            session.ConfirmedItems,
            session.TrySomethingNew);
        await recommendationsProducer.ProduceAsync(KafkaTopicNames.RecommendationsRequested, evt, cancellationToken);
    }

    public async Task<IReadOnlyList<string>?> GetRecommendationsAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        return session?.RankedItems;
    }

    private async Task<MenuSession?> GetOwnedSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null || session.UserId != userId)
            return null;
        return session;
    }
}
