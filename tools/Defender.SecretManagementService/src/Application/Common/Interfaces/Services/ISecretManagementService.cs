using Defender.Common.DB.Pagination;
using Defender.Common.Entities.Secrets;

namespace Defender.SecretManagementService.Application.Common.Interfaces.Services;

public interface ISecretManagementService
{
    Task<PagedResult<string>> GetAllSecretKeysAsync(PaginationRequest request);
    Task<MongoSecret> CreateOrUpdateSecretAsync(string secretName, string value);
    Task<string> GetSecretValueAsync(string secretName);
    Task<MongoSecret> GetSecretAsync(string secretName);
    Task DeleteSecretAsync(string secretName);
}
