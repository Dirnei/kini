using Kini.Api.Authentication.Identities;
using Kini.Api.Organizations;
using MongoDB.Driver;

namespace Kini.Api.Authentication.Sessions;

public static class SessionsEndpoints
{
    public static IEndpointRouteBuilder MapSessionsEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/auth").WithTags("auth");

        grp.MapGet("/me", Me).RequireSession();
        grp.MapPost("/sign-out", SignOut).RequireSession();

        return app;
    }

    private static async Task<IResult> Me(
        HttpContext http,
        IdentitiesCollection identities,
        OrganizationsCollection orgs,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var identity = await identities.Collection.Find(i => i.Id == session.IdentityId).FirstOrDefaultAsync(ct);
        var org = await orgs.Collection.Find(o => o.Id == session.OrgId).FirstOrDefaultAsync(ct);

        if (identity is null || org is null) return Results.Unauthorized();

        return Results.Ok(new { identity, organization = org, session });
    }

    private static async Task<IResult> SignOut(
        HttpContext http,
        SessionsCollection sessions,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var update = Builders<Session>.Update.Set(s => s.RevokedAt, DateTimeOffset.UtcNow);
        await sessions.Collection.UpdateOneAsync(s => s.Id == session.Id, update, cancellationToken: ct);
        return Results.NoContent();
    }
}
