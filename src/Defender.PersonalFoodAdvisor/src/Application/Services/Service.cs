using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

namespace Defender.PersonalFoodAdvisor.Application.Services;

public class Service(
    IDomainModelRepository accountInfoRepository) : IService
{
    public Task DoService()
    {
        throw new NotImplementedException();
    }
}
