using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.Entities.Secrets;
using Defender.SecretManagementService.Application.Common.Interfaces.Repositories;
using Defender.SecretManagementService.Application.Common.Interfaces.Services;

namespace Defender.SecretManagementService.Application.Services;

public class SecretManagementService(
    ISecretRepository secretRepository) : ISecretManagementService
{
    public async Task<PagedResult<string>> GetAllSecretKeysAsync(PaginationRequest request)
    {
        var settings = PaginationSettings<MongoSecret>.FromPaginationRequest(request);

        var filterRequest = FindModelRequest<MongoSecret>
            .Init()
            .Sort(x => x.SecretName, SortType.Asc);

        settings.SetupFindOptions(filterRequest);

        var mongoSecrets = await secretRepository.GetMongoSecretsAsync(settings);

        return PagedResult<MongoSecret>.FromPagedResult<string>(mongoSecrets, x => x.SecretName);
    }

    public async Task<MongoSecret> CreateOrUpdateSecretAsync(string secretName, string value)
    {
        return await secretRepository.CreateOrUpdateSecretAsync(secretName, value);
    }

    public async Task<string> GetSecretValueAsync(string secretName)
    {
        return await secretRepository.GetSecretValueByNameAsync(secretName);
    }

    public async Task<MongoSecret> GetSecretAsync(string secretName)
    {
        return await secretRepository.GetSecretByNameAsync(secretName);
    }

    public async Task DeleteSecretAsync(string secretName)
    {
        await secretRepository.DeleteSecretByNameAsync(secretName);
    }
}
