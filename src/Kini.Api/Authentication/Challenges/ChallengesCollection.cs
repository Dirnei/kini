using MongoDB.Driver;

namespace Kini.Api.Authentication.Challenges;

public sealed class ChallengesCollection
{
    public ChallengesCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<Challenge>("challenges");
    }

    public IMongoCollection<Challenge> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            // Auto-expire challenges once their expiresAt passes.
            new CreateIndexModel<Challenge>(
                Builders<Challenge>.IndexKeys.Ascending(c => c.ExpiresAt),
                new CreateIndexOptions { Name = "expiresAt_ttl", ExpireAfter = TimeSpan.Zero }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
