using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Application.Common.Interfaces.Services;

namespace Defender.HealthCareService.Application.Services;

public class Service(
    IHealthEventRepository accountInfoRepository) : IService
{
    public Task DoService()
    {
        throw new NotImplementedException();
    }
}
