using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Authentication.Credentials;

public static class RegisterSshCredential
{
    public sealed record Request(string PublicKey);

    public static async Task<IResult> Handle(
        Request request,
        HttpContext http,
        SshCredentialsCollection credentials,
        CancellationToken ct)
    {
        var session = http.GetSession();

        SshPublicKey parsed;
        try { parsed = SshPublicKey.Parse(request.PublicKey); }
        catch (FormatException fx)
        {
            return Results.BadRequest(new { code = "invalid_public_key", message = fx.Message });
        }

        // Reject if this identity already has the same fingerprint registered.
        var existing = await credentials.Collection
            .Find(c => c.IdentityId == session.IdentityId && c.Fingerprint == parsed.Fingerprint && c.RevokedAt == null)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            return Results.Conflict(new { code = "already_registered", message = "This key is already registered." });

        var cred = new SshCredential(
            Id: Guid.NewGuid(),
            IdentityId: session.IdentityId,
            PublicKey: parsed.Canonical,
            Fingerprint: parsed.Fingerprint,
            Algorithm: parsed.Algorithm,
            Comment: parsed.Comment,
            CreatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null,
            RevokedAt: null);

        await credentials.Collection.InsertOneAsync(cred, cancellationToken: ct);
        return Results.Created($"/v1/auth/credentials/ssh/{cred.Id}", cred);
    }
}
