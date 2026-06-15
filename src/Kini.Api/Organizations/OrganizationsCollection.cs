using MongoDB.Driver;

namespace Kini.Api.Organizations;

public sealed class OrganizationsCollection
{
    public OrganizationsCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<Organization>("orgs");
    }

    public IMongoCollection<Organization> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Organization>(
                Builders<Organization>.IndexKeys.Ascending(o => o.Name),
                new CreateIndexOptions { Name = "name_idx" }),
            // Slug is required, so a plain unique index is fine.
            new CreateIndexModel<Organization>(
                Builders<Organization>.IndexKeys.Ascending(o => o.Slug),
                new CreateIndexOptions { Name = "slug_unique", Unique = true }),
            // PrimaryDomain is optional → sparse unique. Docs without the
            // field aren't indexed, so multiple "no domain" orgs coexist.
            new CreateIndexModel<Organization>(
                Builders<Organization>.IndexKeys.Ascending(o => o.PrimaryDomain),
                new CreateIndexOptions { Name = "primaryDomain_unique", Unique = true, Sparse = true }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public Task<Organization?> FindBySlug(string slug, CancellationToken ct = default) =>
        Collection.Find(o => o.Slug == slug).FirstOrDefaultAsync(ct)!;

    public Task<Organization?> FindByPrimaryDomain(string domain, CancellationToken ct = default) =>
        Collection.Find(o => o.PrimaryDomain == domain).FirstOrDefaultAsync(ct)!;

    public async Task<bool> SlugTaken(string slug, CancellationToken ct = default) =>
        await Collection.Find(o => o.Slug == slug).AnyAsync(ct);

    public async Task<bool> DomainTaken(string domain, CancellationToken ct = default) =>
        await Collection.Find(o => o.PrimaryDomain == domain).AnyAsync(ct);
}
