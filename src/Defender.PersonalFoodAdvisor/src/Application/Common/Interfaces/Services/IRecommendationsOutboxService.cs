using System.Net;
using Defender.PersonalFoodAdvisor.Application.Kafka;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IRecommendationsOutboxService
{
    Task EnqueueAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default);
    Task<bool> ScheduleRetryAsync(RecommendationsRequestedEvent evt, HttpStatusCode? statusCode, CancellationToken cancellationToken = default);
}
