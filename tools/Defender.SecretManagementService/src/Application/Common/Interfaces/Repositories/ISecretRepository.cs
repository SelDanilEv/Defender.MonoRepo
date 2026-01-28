using Defender.Common.DB.Pagination;
using Defender.Common.Entities.Secrets;
using Defender.Common.Interfaces;

namespace Defender.SecretManagementService.Application.Common.Interfaces.Repositories;

public interface ISecretRepository : IMongoSecretAccessor
{
    Task<PagedResult<MongoSecret>> GetMongoSecretsAsync(PaginationSettings<MongoSecret> paginationSettings);
    Task<MongoSecret> CreateOrUpdateSecretAsync(string secretName, string value);
    Task DeleteSecretByNameAsync(string secretName);
}
