using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Keys;

public static class RevokeKey
{
    public sealed record Request(string? Reason);

    public static async Task<IResult> Handle(
        Guid keyId,
        Request? request,
        HttpContext http,
        KeysCollection keys,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var update = Builders<Key>.Update
            .Set(k => k.RevokedAt, DateTimeOffset.UtcNow)
            .Set(k => k.RevocationReason, request?.Reason);

        var result = await keys.Collection.FindOneAndUpdateAsync(
            k => k.Id == keyId && k.OrgId == session.OrgId,
            update,
            new FindOneAndUpdateOptions<Key> { ReturnDocument = ReturnDocument.After },
            ct);

        if (result is null) return Results.NotFound();
        return Results.Ok(result);
    }

    public static async Task<IResult> Delete(
        Guid keyId,
        HttpContext http,
        KeysCollection keys,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var result = await keys.Collection.DeleteOneAsync(
            k => k.Id == keyId && k.OrgId == session.OrgId, ct);
        return result.DeletedCount == 0 ? Results.NotFound() : Results.NoContent();
    }
}
