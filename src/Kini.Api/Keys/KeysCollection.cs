using MongoDB.Driver;

namespace Kini.Api.Keys;

public sealed class KeysCollection
{
    public KeysCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<Key>("keys");
    }

    public IMongoCollection<Key> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            // Same identity can't publish the same key twice. Unique within an identity,
            // not globally — different users can publish the same pubkey if they really
            // want to (e.g., shared CI key).
            new CreateIndexModel<Key>(
                Builders<Key>.IndexKeys
                    .Ascending(k => k.IdentityId)
                    .Ascending(k => k.Fingerprint),
                new CreateIndexOptions { Name = "identityId_fingerprint_unique", Unique = true }),
            new CreateIndexModel<Key>(
                Builders<Key>.IndexKeys.Ascending(k => k.OrgId),
                new CreateIndexOptions { Name = "orgId_idx" }),
            new CreateIndexModel<Key>(
                Builders<Key>.IndexKeys.Ascending(k => k.Type),
                new CreateIndexOptions { Name = "type_idx" }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public Task<List<Key>> ActiveForIdentity(Guid identityId, string? type, CancellationToken ct = default)
    {
        var filter = Builders<Key>.Filter.And(
            Builders<Key>.Filter.Eq(k => k.IdentityId, identityId),
            Builders<Key>.Filter.Eq(k => k.RevokedAt, null));
        if (!string.IsNullOrEmpty(type))
            filter = Builders<Key>.Filter.And(filter, Builders<Key>.Filter.Eq(k => k.Type, type));
        return Collection.Find(filter).ToListAsync(ct);
    }
}
