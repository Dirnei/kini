using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Authentication.Credentials;

public static class CredentialsEndpoints
{
    public static IEndpointRouteBuilder MapCredentialsEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/auth/credentials").WithTags("credentials");

        // --- SSH ---
        grp.MapGet("/ssh", ListSsh).RequireSession();
        grp.MapPost("/ssh", RegisterSshCredential.Handle).RequireSession();
        grp.MapDelete("/ssh/{credentialId:guid}", RevokeSsh).RequireSession();

        // --- WebAuthn ---
        grp.MapGet("/webauthn", ListWebAuthn).RequireSession();
        grp.MapPost("/webauthn/begin-registration", BeginWebAuthnRegistration.Handle).RequireSession();
        grp.MapPost("/webauthn/complete-registration", CompleteWebAuthnRegistration.Handle).RequireSession();
        grp.MapDelete("/webauthn/{credentialId:guid}", RevokeWebAuthn).RequireSession();

        return app;
    }

    private static async Task<IResult> ListSsh(HttpContext http, SshCredentialsCollection creds, CancellationToken ct)
    {
        var session = http.GetSession();
        var list = await creds.ActiveForIdentity(session.IdentityId, ct);
        return Results.Ok(list);
    }

    private static async Task<IResult> RevokeSsh(Guid credentialId, HttpContext http, SshCredentialsCollection creds, CancellationToken ct)
    {
        var session = http.GetSession();
        var update = Builders<SshCredential>.Update.Set(c => c.RevokedAt, DateTimeOffset.UtcNow);
        var result = await creds.Collection.UpdateOneAsync(
            c => c.Id == credentialId && c.IdentityId == session.IdentityId,
            update,
            cancellationToken: ct);
        return result.MatchedCount == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> ListWebAuthn(HttpContext http, WebAuthnCredentialsCollection creds, CancellationToken ct)
    {
        var session = http.GetSession();
        var list = await creds.ActiveForIdentity(session.IdentityId, ct);
        return Results.Ok(list);
    }

    private static async Task<IResult> RevokeWebAuthn(Guid credentialId, HttpContext http, WebAuthnCredentialsCollection creds, CancellationToken ct)
    {
        var session = http.GetSession();
        var update = Builders<WebAuthnCredential>.Update.Set(c => c.RevokedAt, DateTimeOffset.UtcNow);
        var result = await creds.Collection.UpdateOneAsync(
            c => c.Id == credentialId && c.IdentityId == session.IdentityId,
            update,
            cancellationToken: ct);
        return result.MatchedCount == 0 ? Results.NotFound() : Results.NoContent();
    }
}
