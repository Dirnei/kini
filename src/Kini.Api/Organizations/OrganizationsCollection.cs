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
        };

        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
