using MongoDB.Driver;

namespace Kini.Api.Authentication.Sessions;

public sealed class SessionsCollection
{
    public SessionsCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<Session>("sessions");
    }

    public IMongoCollection<Session> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Session>(
                Builders<Session>.IndexKeys.Ascending(s => s.TokenHash),
                new CreateIndexOptions { Name = "tokenHash_unique", Unique = true }),
            new CreateIndexModel<Session>(
                Builders<Session>.IndexKeys.Ascending(s => s.IdentityId),
                new CreateIndexOptions { Name = "identityId_idx" }),
            // MongoDB will auto-expire sessions past their expiry. expireAfterSeconds: 0
            // means "remove once the indexed date is in the past."
            new CreateIndexModel<Session>(
                Builders<Session>.IndexKeys.Ascending(s => s.ExpiresAt),
                new CreateIndexOptions { Name = "expiresAt_ttl", ExpireAfter = TimeSpan.Zero }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public Task<Session?> FindByTokenHash(string tokenHash, CancellationToken ct = default) =>
        Collection.Find(s => s.TokenHash == tokenHash).FirstOrDefaultAsync(ct)!;
}
