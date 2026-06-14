using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Authentication.Identities;

public static class ListIdentities
{
    public static async Task<IResult> Handle(
        Guid orgId,
        HttpContext http,
        IdentitiesCollection identities,
        CancellationToken ct)
    {
        var session = http.GetSession();
        if (session.OrgId != orgId)
            return Results.Forbid();

        var docs = await identities.Collection
            .Find(i => i.OrgId == orgId)
            .ToListAsync(ct);

        return Results.Ok(docs);
    }
}
