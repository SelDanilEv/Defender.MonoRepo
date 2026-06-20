using Defender.HealthCareService.Domain.Entities;

namespace Defender.HealthCareService.Application.Common.Interfaces.Repositories;

public interface IHealthEventRepository
{
    Task<HealthEvent> GetHealthEventByIdAsync(Guid id);
    Task<IReadOnlyList<HealthEvent>> GetHealthEventsAsync(Guid userId, DateTimeOffset? from, DateTimeOffset? to);
    Task<HealthEvent?> GetHealthEventByIdAsync(Guid userId, Guid id);
    Task<HealthEvent> AddHealthEventAsync(HealthEvent healthEvent);
    Task<HealthEvent> UpdateHealthEventAsync(HealthEvent healthEvent);
    Task<bool> DeleteHealthEventAsync(Guid userId, Guid id);
}

public interface IHealthChartShareRepository
{
    Task<HealthChartShare> AddHealthChartShareAsync(HealthChartShare share);
    Task<HealthChartShare?> GetHealthChartShareByTokenAsync(string token);
}
