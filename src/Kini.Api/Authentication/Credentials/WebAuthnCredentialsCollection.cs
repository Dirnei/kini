using MongoDB.Driver;

namespace Kini.Api.Authentication.Credentials;

public sealed class WebAuthnCredentialsCollection
{
    public WebAuthnCredentialsCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<WebAuthnCredential>("webauthn_credentials");
    }

    public IMongoCollection<WebAuthnCredential> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<WebAuthnCredential>(
                Builders<WebAuthnCredential>.IndexKeys.Ascending(c => c.IdentityId),
                new CreateIndexOptions { Name = "identityId_idx" }),
            new CreateIndexModel<WebAuthnCredential>(
                Builders<WebAuthnCredential>.IndexKeys.Ascending(c => c.CredentialId),
                new CreateIndexOptions { Name = "credentialId_unique", Unique = true }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public Task<List<WebAuthnCredential>> ActiveForIdentity(Guid identityId, CancellationToken ct = default) =>
        Collection
            .Find(c => c.IdentityId == identityId && c.RevokedAt == null)
            .ToListAsync(ct);

    public Task<WebAuthnCredential?> FindByCredentialId(string credentialId, CancellationToken ct = default) =>
        Collection.Find(c => c.CredentialId == credentialId).FirstOrDefaultAsync(ct)!;
}
