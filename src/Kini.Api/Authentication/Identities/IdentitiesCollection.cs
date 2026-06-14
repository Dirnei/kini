using MongoDB.Driver;

namespace Kini.Api.Authentication.Identities;

public sealed class IdentitiesCollection
{
    public IdentitiesCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<Identity>("identities");
    }

    public IMongoCollection<Identity> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Identity>(
                Builders<Identity>.IndexKeys.Ascending(i => i.Email),
                new CreateIndexOptions { Name = "email_unique", Unique = true }),
            new CreateIndexModel<Identity>(
                Builders<Identity>.IndexKeys.Ascending(i => i.Username),
                new CreateIndexOptions { Name = "username_unique", Unique = true }),
            new CreateIndexModel<Identity>(
                Builders<Identity>.IndexKeys.Ascending(i => i.OrgId),
                new CreateIndexOptions { Name = "orgId_idx" }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public Task<Identity?> FindByEmail(string email, CancellationToken ct = default) =>
        Collection.Find(i => i.Email == email).FirstOrDefaultAsync(ct)!;

    public Task<Identity?> FindByUsername(string username, CancellationToken ct = default) =>
        Collection.Find(i => i.Username == username).FirstOrDefaultAsync(ct)!;

    public Task<Identity?> FindById(Guid id, CancellationToken ct = default) =>
        Collection.Find(i => i.Id == id).FirstOrDefaultAsync(ct)!;
}
