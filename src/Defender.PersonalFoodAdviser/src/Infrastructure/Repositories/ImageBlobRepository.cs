using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Infrastructure.Repositories;

public class ImageBlobRepository : BaseMongoRepository<ImageBlob>, IImageBlobRepository
{
    public ImageBlobRepository(IOptions<MongoDbOptions> mongoOption)
        : base(mongoOption.Value, "ImageBlob")
    {
    }

    public async Task<ImageBlob> SaveAsync(ImageBlob blob, CancellationToken cancellationToken = default)
    {
        if (blob.Id == Guid.Empty)
            blob.Id = Guid.NewGuid();
        return await AddItemAsync(blob);
    }

    public async Task<ImageBlob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync(id);
    }
}
