using Kini.Api.Audit;
using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;
using Kini.Api.Keys;
using MongoDB.Driver;

namespace Kini.Api.Authentication.SignIn;

public static class VerifySshChallenge
{
    public sealed record Request(string Email, string Nonce, string Signature);

    public static async Task<IResult> Handle(
        Request request,
        IdentitiesCollection identities,
        SshCredentialsCollection credentials,
        KeysCollection publishedKeys,
        ChallengeStore challenges,
        SshSignatureVerifier verifier,
        IssueSession sessions,
        AuditLog audit,
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

        // 3b. Lazy-claim fallback: if no SshCredential matched, the user may
        //     still be signing in with one of their PUBLISHED keys — that's
        //     how "an admin uploaded a key for me but I never registered"
        //     turns into a real session. On success, persist the published
        //     key as a credential so future sign-ins are direct.
        if (matched is null)
        {
            var pubs = await publishedKeys.ActiveForIdentity(identity.Id, type: "ssh", ct);
            foreach (var pub in pubs)
            {
                if (await verifier.Verify(request.Nonce, request.Signature, identity.Email, pub.PublicKey, ct))
                {
                    var lazyCred = new SshCredential(
                        Id: Guid.NewGuid(),
                        IdentityId: identity.Id,
                        PublicKey: pub.PublicKey,
                        Fingerprint: pub.Fingerprint,
                        Algorithm: pub.Algorithm,
                        Comment: pub.Comment,
                        CreatedAt: DateTimeOffset.UtcNow,
                        LastUsedAt: DateTimeOffset.UtcNow,
                        RevokedAt: null);
                    await credentials.Collection.InsertOneAsync(lazyCred, cancellationToken: ct);
                    matched = lazyCred;
                    break;
                }
            }
        }

        if (matched is null)
        {
            await audit.RecordAs(identity.OrgId,
                new AuditActor("user", identity.Id, identity.Email),
                AuditAction.SignInFailed,
                new AuditTarget("identity", identity.Id, identity.Email),
                new Dictionary<string, string> { ["method"] = "ssh" }, ct);
            return Results.Unauthorized();
        }

        // 4. Mint a session, mark the credential as recently used.
        var (session, plaintext) = await sessions.ForIdentity(identity, ct);
        await credentials.Collection.UpdateOneAsync(
            c => c.Id == matched.Id,
            Builders<SshCredential>.Update.Set(c => c.LastUsedAt, DateTimeOffset.UtcNow),
            cancellationToken: ct);

        await audit.RecordAs(identity.OrgId,
            new AuditActor("user", identity.Id, identity.Email),
            AuditAction.SignInSucceeded,
            new AuditTarget("identity", identity.Id, identity.Email),
            new Dictionary<string, string> { ["method"] = "ssh", ["fingerprint"] = matched.Fingerprint }, ct);

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
