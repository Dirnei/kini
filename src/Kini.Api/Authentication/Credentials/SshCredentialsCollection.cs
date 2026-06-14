using MongoDB.Driver;

namespace Kini.Api.Authentication.Credentials;

public sealed class SshCredentialsCollection
{
    public SshCredentialsCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<SshCredential>("ssh_credentials");
    }

    public IMongoCollection<SshCredential> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<SshCredential>(
                Builders<SshCredential>.IndexKeys.Ascending(c => c.IdentityId),
                new CreateIndexOptions { Name = "identityId_idx" }),
            new CreateIndexModel<SshCredential>(
                Builders<SshCredential>.IndexKeys.Ascending(c => c.Fingerprint),
                new CreateIndexOptions { Name = "fingerprint_idx" }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public Task<List<SshCredential>> ActiveForIdentity(Guid identityId, CancellationToken ct = default) =>
        Collection
            .Find(c => c.IdentityId == identityId && c.RevokedAt == null)
            .ToListAsync(ct);
}
