using MongoDB.Driver;

namespace Kini.Api.Audit;

public sealed class AuditCollection
{
    public AuditCollection(IMongoDatabase db)
    {
        Collection = db.GetCollection<AuditEvent>("audit");
    }

    public IMongoCollection<AuditEvent> Collection { get; }

    public Task EnsureIndexes(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<AuditEvent>(
                Builders<AuditEvent>.IndexKeys
                    .Ascending(e => e.OrgId)
                    .Descending(e => e.OccurredAt),
                new CreateIndexOptions { Name = "orgId_occurredAt_idx" }),
        };
        return Collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
