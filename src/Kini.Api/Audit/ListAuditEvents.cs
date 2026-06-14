using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Audit;

public static class ListAuditEvents
{
    public sealed record Page(IReadOnlyList<AuditEvent> Items, string? NextCursor);

    public static async Task<IResult> Handle(
        HttpContext http,
        AuditCollection audit,
        DateTimeOffset? since,
        int? limit,
        string? cursor,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var take = Math.Clamp(limit ?? 100, 1, 500);

        var filterB = Builders<AuditEvent>.Filter.Eq(e => e.OrgId, session.OrgId);
        if (since is { } s)
            filterB = Builders<AuditEvent>.Filter.And(filterB, Builders<AuditEvent>.Filter.Gte(e => e.OccurredAt, s));
        if (!string.IsNullOrEmpty(cursor) && DateTimeOffset.TryParse(cursor, out var cur))
            filterB = Builders<AuditEvent>.Filter.And(filterB, Builders<AuditEvent>.Filter.Lt(e => e.OccurredAt, cur));

        var docs = await audit.Collection
            .Find(filterB)
            .Sort(Builders<AuditEvent>.Sort.Descending(e => e.OccurredAt))
            .Limit(take + 1)
            .ToListAsync(ct);

        string? next = null;
        if (docs.Count > take)
        {
            next = docs[take - 1].OccurredAt.ToString("O");
            docs.RemoveAt(docs.Count - 1);
        }

        return Results.Ok(new Page(docs, next));
    }
}
