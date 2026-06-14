using System.Buffers.Text;
using System.Text.Json;
using Fido2NetLib;
using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Authentication.SignIn;

public static class CompleteWebAuthnAssertion
{
    public sealed record Request(string CeremonyId, JsonElement Assertion);

    public static async Task<IResult> Handle(
        Request request,
        IFido2 fido2,
        IdentitiesCollection identities,
        WebAuthnCredentialsCollection credentials,
        ChallengeStore challenges,
        IssueSession sessions,
        CancellationToken ct)
    {
        if (!Guid.TryParse(request.CeremonyId, out var ceremonyGuid))
            return Results.BadRequest(new { code = "invalid_ceremony" });

        var challenge = await challenges.Consume(ChallengeKind.WebAuthnAssertion, ceremonyGuid, ct);
        if (challenge is null)
            return Results.Unauthorized();

        var options = JsonSerializer.Deserialize<AssertionOptions>(challenge.Payload)
            ?? throw new InvalidOperationException("Stored assertion options could not be deserialized.");

        var assertionResponse =
            JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(request.Assertion.GetRawText())
            ?? throw new ArgumentException("assertion could not be parsed.", nameof(request));

        // AuthenticatorAssertionRawResponse.Id is already the browser-provided
        // base64url-encoded credential id — no re-encoding needed.
        var credentialIdBase64 = assertionResponse.Id;
        var storedCred = await credentials.FindByCredentialId(credentialIdBase64, ct);
        if (storedCred is null || storedCred.RevokedAt is not null)
            return Results.Unauthorized();

        var identity = await identities.FindById(storedCred.IdentityId, ct);
        if (identity is null) return Results.Unauthorized();

        // Same email must have been claimed at /begin (if any was). Defence-in-depth.
        if (challenge.Email is not null &&
            !string.Equals(challenge.Email, identity.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertionResponse,
            OriginalOptions = options,
            StoredPublicKey = storedCred.PublicKey,
            StoredSignatureCounter = (uint)storedCred.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = (args, _) =>
            {
                var idMatches = new Guid(args.UserHandle) == identity.Id;
                return Task.FromResult(idMatches);
            },
        }, ct);

        // Update sign count + lastUsedAt.
        var update = Builders<WebAuthnCredential>.Update
            .Set(c => c.SignCount, (long)result.SignCount)
            .Set(c => c.LastUsedAt, DateTimeOffset.UtcNow);
        await credentials.Collection.UpdateOneAsync(c => c.Id == storedCred.Id, update, cancellationToken: ct);

        var (session, plaintext) = await sessions.ForIdentity(identity, ct);

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
