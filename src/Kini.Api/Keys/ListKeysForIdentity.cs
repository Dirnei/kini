using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Keys;

public static class ListKeysForIdentity
{
    public static async Task<IResult> Handle(
        string email,
        bool? includeRevoked,
        string? type,
        HttpContext http,
        IdentitiesCollection identities,
        KeysCollection keys,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var identity = await identities.FindByEmail(email.Trim().ToLowerInvariant(), ct);
        if (identity is null || identity.OrgId != session.OrgId)
            return Results.NotFound();

        var filter = Builders<Key>.Filter.Eq(k => k.IdentityId, identity.Id);
        if (includeRevoked != true)
            filter = Builders<Key>.Filter.And(filter, Builders<Key>.Filter.Eq(k => k.RevokedAt, null));
        if (!string.IsNullOrEmpty(type))
            filter = Builders<Key>.Filter.And(filter, Builders<Key>.Filter.Eq(k => k.Type, type));

        var docs = await keys.Collection.Find(filter).ToListAsync(ct);
        return Results.Ok(docs);
    }
}
