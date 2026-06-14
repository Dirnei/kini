using MongoDB.Driver;

namespace Kini.Api.ApiTokens;

public sealed class ApiTokensCollection
{
    public ApiTokensCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<ApiToken>("api_tokens");
    }

    public IMongoCollection<ApiToken> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<ApiToken>(
                Builders<ApiToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Name = "tokenHash_unique", Unique = true }),
            new CreateIndexModel<ApiToken>(
                Builders<ApiToken>.IndexKeys.Ascending(t => t.IdentityId),
                new CreateIndexOptions { Name = "identityId_idx" }),
            new CreateIndexModel<ApiToken>(
                Builders<ApiToken>.IndexKeys.Ascending(t => t.OrgId),
                new CreateIndexOptions { Name = "orgId_idx" }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public Task<ApiToken?> FindByTokenHash(string hash, CancellationToken ct = default) =>
        Collection.Find(t => t.TokenHash == hash).FirstOrDefaultAsync(ct)!;
}
