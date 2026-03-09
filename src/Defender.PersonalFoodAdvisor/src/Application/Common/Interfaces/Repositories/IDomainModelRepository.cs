using Defender.PersonalFoodAdvisor.Domain.Entities;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;

public interface IDomainModelRepository
{
    Task<DomainModel> GetDomainModelByIdAsync(Guid id);
}
