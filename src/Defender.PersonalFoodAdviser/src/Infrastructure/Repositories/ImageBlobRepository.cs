using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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

    public async Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _mongoCollection.DeleteManyAsync(
            Builders<ImageBlob>.Filter.Eq(blob => blob.SessionId, sessionId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> FindSessionIdsByExactImageHashesAsync(
        IReadOnlyList<string> imageHashes,
        Guid excludedSessionId,
        CancellationToken cancellationToken = default)
    {
        var normalizedHashes = NormalizeHashes(imageHashes);
        if (normalizedHashes.Count == 0)
            return [];

        var distinctHashes = normalizedHashes
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var matchingBlobsFilter = Builders<ImageBlob>.Filter.And(
            Builders<ImageBlob>.Filter.Ne(blob => blob.SessionId, excludedSessionId),
            Builders<ImageBlob>.Filter.In(blob => blob.ImageHash, distinctHashes));

        var matchingBlobs = await _mongoCollection
            .Find(matchingBlobsFilter)
            .ToListAsync(cancellationToken);

        var candidateSessionIds = matchingBlobs
            .Where(blob => !string.IsNullOrWhiteSpace(blob.ImageHash))
            .Select(blob => blob.SessionId)
            .Distinct()
            .ToList();

        if (candidateSessionIds.Count == 0)
            return [];

        var candidateBlobs = await _mongoCollection
            .Find(Builders<ImageBlob>.Filter.In(blob => blob.SessionId, candidateSessionIds))
            .ToListAsync(cancellationToken);

        return candidateBlobs
            .GroupBy(blob => blob.SessionId)
            .Where(group =>
                group.Count() == normalizedHashes.Count &&
                AreEquivalentHashes(
                    normalizedHashes,
                    NormalizeHashes(group.Select(blob => blob.ImageHash))))
            .Select(group => group.Key)
            .ToList();
    }

    private static List<string> NormalizeHashes(IEnumerable<string?> imageHashes)
    {
        var normalized = new List<string>();

        foreach (var imageHash in imageHashes)
        {
            var value = imageHash?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            normalized.Add(value.ToUpperInvariant());
        }

        return normalized;
    }

    private static bool AreEquivalentHashes(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
            return false;

        var orderedLeft = left.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var orderedRight = right.OrderBy(value => value, StringComparer.Ordinal).ToArray();

        for (var i = 0; i < orderedLeft.Length; i++)
        {
            if (!string.Equals(orderedLeft[i], orderedRight[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
