using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Infrastructure.Repositories;

public class DomainModelRepository : BaseMongoRepository<DomainModel>, IDomainModelRepository
{
    public DomainModelRepository(IOptions<MongoDbOptions> mongoOption) : base(mongoOption.Value)
    {
    }

    public async Task<DomainModel> GetDomainModelByIdAsync(Guid id)
    {
        return await GetItemAsync(id);
    }
}
