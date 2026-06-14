using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Keys;

public static class GetKey
{
    public static async Task<IResult> Handle(
        Guid keyId,
        HttpContext http,
        KeysCollection keys,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var key = await keys.Collection.Find(k => k.Id == keyId).FirstOrDefaultAsync(ct);
        if (key is null || key.OrgId != session.OrgId) return Results.NotFound();
        return Results.Ok(key);
    }
}
