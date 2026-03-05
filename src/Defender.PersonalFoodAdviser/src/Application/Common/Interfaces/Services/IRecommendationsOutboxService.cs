using System.Net;
using Defender.PersonalFoodAdviser.Application.Kafka;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IRecommendationsOutboxService
{
    Task EnqueueAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default);
    Task<bool> ScheduleRetryAsync(RecommendationsRequestedEvent evt, HttpStatusCode? statusCode, CancellationToken cancellationToken = default);
}
