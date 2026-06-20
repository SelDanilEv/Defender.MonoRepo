using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.HealthCareService.Infrastructure.Repositories;

public class HealthEventRepository : BaseMongoRepository<HealthEvent>, IHealthEventRepository
{
    public HealthEventRepository(IOptions<MongoDbOptions> mongoOption) : base(mongoOption.Value)
    {
    }

    public async Task<HealthEvent> GetHealthEventByIdAsync(Guid id)
    {
        return await GetItemAsync(id);
    }
}
