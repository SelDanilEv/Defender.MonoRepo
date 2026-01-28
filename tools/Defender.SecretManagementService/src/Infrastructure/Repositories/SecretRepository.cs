using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.DB.Repositories;
using Defender.Common.Entities.Secrets;
using Defender.Common.Helpers;
using Defender.Common.Interfaces;
using Defender.SecretManagementService.Application.Common.Interfaces.Repositories;
using Microsoft.Extensions.Options;

namespace Defender.SecretManagementService.Infrastructure.Repositories;

public class SecretRepository : BaseMongoRepository<MongoSecret>, ISecretRepository
{
    private readonly IMongoSecretAccessor _secretAccessor;

    public SecretRepository(
        IOptions<MongoDbOptions> mongoOption,
        IMongoSecretAccessor secretAccessor)
        : base(mongoOption?.Value)
    {
        _secretAccessor = secretAccessor;
    }

    public async Task<PagedResult<MongoSecret>> GetMongoSecretsAsync(
        PaginationSettings<MongoSecret> settings)
    {
        return await GetItemsAsync(settings);
    }


    public async Task<MongoSecret> CreateOrUpdateSecretAsync(string secretName, string value)
    {
        value = await CryptographyHelper.EncryptStringAsync(value, secretName);

        var existingSecret = await GetSecretByNameAsync(secretName);

        if (existingSecret == null)
        {
            var secret = MongoSecret.FromSecretName(secretName, value);

            return await AddItemAsync(secret);
        }

        var updateRequest = UpdateModelRequest<MongoSecret>
            .Init(existingSecret)
            .Set(x => x.Value, value);

        return await UpdateItemAsync(updateRequest);
    }

    public async Task<string> GetSecretValueByNameAsync(string secretName)
    {
        return await _secretAccessor.GetSecretValueByNameAsync(secretName);
    }

    public async Task<MongoSecret> GetSecretByNameAsync(string secretName)
    {
        return await _secretAccessor.GetSecretByNameAsync(secretName);
    }

    public async Task DeleteSecretByNameAsync(string secretName)
    {
        var secret = await GetSecretByNameAsync(secretName);

        await RemoveItemAsync(secret.Id);
    }
}
