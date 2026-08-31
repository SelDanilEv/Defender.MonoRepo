using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Repositories;
using Microsoft.Extensions.Options;

namespace Defender.BudgetTracker.Infrastructure.Repositories;

public class RegularExpenseDiagramSetupRepository
    : BaseMongoRepository<RegularExpenseDiagramSetup>, IRegularExpenseDiagramSetupRepository
{
    public RegularExpenseDiagramSetupRepository(IOptions<MongoDbOptions> mongoOption)
        : base(mongoOption.Value, "RegularExpenseDiagramSetups")
    {
    }

    public async Task<RegularExpenseDiagramSetup?> GetRegularExpenseDiagramSetupByUserIdAsync(Guid userId)
    {
        var filter = FindModelRequest<RegularExpenseDiagramSetup>
            .Init(setup => setup.UserId, userId);

        return await GetItemAsync(filter);
    }

    public Task<RegularExpenseDiagramSetup> SetRegularExpenseDiagramSetupAsync(
        RegularExpenseDiagramSetup setup)
    {
        var filter = FindModelRequest<RegularExpenseDiagramSetup>
            .Init(existing => existing.UserId, setup.UserId);

        return ReplaceItemAsync(setup, filter);
    }
}
