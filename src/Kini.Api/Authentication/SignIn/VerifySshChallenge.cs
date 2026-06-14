using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Authentication.SignIn;

public static class VerifySshChallenge
{
    public sealed record Request(string Email, string Nonce, string Signature);

    public static async Task<IResult> Handle(
        Request request,
        IdentitiesCollection identities,
        SshCredentialsCollection credentials,
        ChallengeStore challenges,
        SshSignatureVerifier verifier,
        IssueSession sessions,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Nonce) ||
            string.IsNullOrWhiteSpace(request.Signature))
        {
            return Results.BadRequest(new { code = "missing_fields" });
        }

        // 1. The challenge must exist, match email+nonce, be unexpired and unused.
        var challenge = await challenges.ConsumeByEmailAndNonce(
            ChallengeKind.Ssh, request.Email, request.Nonce, ct);
        if (challenge is null)
            return Results.Unauthorized();

        // 2. There must be an identity for this email.
        var identity = await identities.FindByEmail(request.Email, ct);
        if (identity is null)
            return Results.Unauthorized();

        // 3. Walk the identity's active SSH credentials; verify against each
        //    until one matches. Most identities have one key; some have many.
        var live = await credentials.ActiveForIdentity(identity.Id, ct);
        SshCredential? matched = null;
        foreach (var cred in live)
        {
            if (await verifier.Verify(
                    payload: request.Nonce,
                    signatureArmored: request.Signature,
                    allowedIdentity: identity.Email,
                    allowedPublicKey: cred.PublicKey,
                    ct: ct))
            {
                matched = cred;
                break;
            }
        }

        if (matched is null)
            return Results.Unauthorized();

        // 4. Mint a session, mark the credential as recently used.
        var (session, plaintext) = await sessions.ForIdentity(identity, ct);
        await credentials.Collection.UpdateOneAsync(
            c => c.Id == matched.Id,
            Builders<SshCredential>.Update.Set(c => c.LastUsedAt, DateTimeOffset.UtcNow),
            cancellationToken: ct);

        return Results.Ok(new
        {
            session.Id,
            session.IdentityId,
            session.OrgId,
            session.CreatedAt,
            session.ExpiresAt,
            session.RevokedAt,
            token = plaintext,
        });
    }
}
