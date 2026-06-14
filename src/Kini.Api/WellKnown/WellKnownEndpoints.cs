using System.Text;
using Kini.Api.Authentication.Identities;
using Kini.Api.Keys;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Kini.Api.WellKnown;

public static class WellKnownEndpoints
{
    public static IEndpointRouteBuilder MapWellKnownEndpoints(this IEndpointRouteBuilder app)
    {
        // Public, unauthenticated, byte-shaped responses.
        app.MapGet("/{user}.keys", GetUserDotKeys).WithTags("well-known");
        app.MapGet("/{user}.gpg", GetUserDotGpg).WithTags("well-known");
        app.MapGet("/.well-known/openpgpkey/hu/{localHash}", GetWkdDirect).WithTags("well-known");
        app.MapGet("/.well-known/openpgpkey/{domain}/hu/{localHash}", GetWkdAdvanced).WithTags("well-known");
        return app;
    }

    /// <summary>
    /// GitHub-style SSH keys endpoint. Returns one authorized_keys-format
    /// line per active SSH key, newline-separated.
    /// </summary>
    private static async Task<IResult> GetUserDotKeys(
        string user,
        HttpContext http,
        IdentitiesCollection identities,
        KeysCollection keys,
        CancellationToken ct)
    {
        var identity = await ResolveIdentity(user, http, identities, ct);
        if (identity is null) return Results.NotFound();

        var docs = await keys.ActiveForIdentity(identity.Id, type: "ssh", ct);
        var body = string.Join('\n', docs.Select(k => k.PublicKey)) + (docs.Count > 0 ? "\n" : "");
        return Results.Text(body, "text/plain; charset=utf-8");
    }

    private static Task<IResult> GetUserDotGpg(string user) =>
        // GPG publishing isn't wired yet. The endpoint exists per spec.
        Task.FromResult(Results.NotFound());

    private static Task<IResult> GetWkdDirect(string localHash) =>
        Task.FromResult(Results.NotFound());

    private static Task<IResult> GetWkdAdvanced(string domain, string localHash) =>
        Task.FromResult(Results.NotFound());

    /// <summary>
    /// Resolve <c>{user}</c> from a well-known URL to an Identity. Username
    /// is the primary public identifier — globally unique, chosen at sign-up,
    /// distinct from the email (which WKD requires and which we can't repurpose).
    /// </summary>
    private static Task<Identity?> ResolveIdentity(
        string user,
        HttpContext http,
        IdentitiesCollection identities,
        CancellationToken ct) =>
        identities.FindByUsername(user.Trim().ToLowerInvariant(), ct);
}
