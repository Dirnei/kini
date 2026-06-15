using MongoDB.Bson;
using MongoDB.Driver;

namespace Kini.Api.Mongo;

/// <summary>
/// One-shot migration: when the <c>username</c> field was introduced, existing
/// identities had only <c>email</c>. We derive a sensible default username
/// from the email's local-part, appending a numeric suffix on collision.
///
/// Idempotent: subsequent startups find no docs missing <c>username</c> and
/// do nothing. Cheap to run on every boot; deleted once the schema is stable.
/// </summary>
public static class IdentitiesBackfill
{
    public static async Task BackfillUsernames(IMongoDatabase db, CancellationToken ct = default)
    {
        var coll = db.GetCollection<BsonDocument>("identities");
        var filter = Builders<BsonDocument>.Filter.Exists("username", false);
        using var cursor = await coll.FindAsync(filter, cancellationToken: ct);

        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var doc in cursor.Current)
            {
                var id = doc["_id"];
                var email = doc.TryGetValue("email", out var e) && e.IsString ? e.AsString : "";
                var local = email.Split('@', 2)[0].ToLowerInvariant();
                if (string.IsNullOrEmpty(local)) local = $"user-{Guid.NewGuid():N}".Substring(0, 8);

                var candidate = local;
                var suffix = 2;
                while (await coll.Find(Builders<BsonDocument>.Filter.Eq("username", candidate)).AnyAsync(ct))
                {
                    candidate = $"{local}-{suffix++}";
                    if (suffix > 1000) throw new InvalidOperationException("Backfill could not assign a username after 1000 attempts.");
                }

                await coll.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", id),
                    Builders<BsonDocument>.Update.Set("username", candidate),
                    cancellationToken: ct);
            }
        }
    }

    /// <summary>
    /// Pre-role identities get <see cref="IdentityRole.Owner"/> — they pre-date
    /// the role system and they ARE the org's bootstrap user.
    /// </summary>
    public static Task BackfillRoles(IMongoDatabase db, CancellationToken ct = default)
    {
        var coll = db.GetCollection<BsonDocument>("identities");
        return coll.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Exists("role", false),
            Builders<BsonDocument>.Update.Set("role", "owner"),
            cancellationToken: ct);
    }

}
