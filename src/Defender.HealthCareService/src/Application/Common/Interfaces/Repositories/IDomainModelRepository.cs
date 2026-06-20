using Defender.HealthCareService.Domain.Entities;

namespace Defender.HealthCareService.Application.Common.Interfaces.Repositories;

public interface IHealthEventRepository
{
    Task<HealthEvent> GetHealthEventByIdAsync(Guid id);
}
