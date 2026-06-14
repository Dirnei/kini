using System.Buffers.Text;
using System.Text.Json;
using Fido2NetLib;
using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Authentication.Credentials;

public static class CompleteWebAuthnRegistration
{
    public sealed record Request(string CeremonyId, string? Nickname, JsonElement Attestation);

    public static async Task<IResult> Handle(
        Request request,
        HttpContext http,
        IFido2 fido2,
        WebAuthnCredentialsCollection credentials,
        ChallengeStore challenges,
        CancellationToken ct)
    {
        var session = http.GetSession();

        if (!Guid.TryParse(request.CeremonyId, out var ceremonyGuid))
            return Results.BadRequest(new { code = "invalid_ceremony", message = "ceremonyId is not a uuid." });

        var challenge = await challenges.Consume(ChallengeKind.WebAuthnRegistration, ceremonyGuid, ct);
        if (challenge is null || challenge.IdentityId != session.IdentityId)
            return Results.BadRequest(new { code = "challenge_invalid", message = "Challenge not found or expired." });

        var options = JsonSerializer.Deserialize<CredentialCreateOptions>(challenge.Payload)
            ?? throw new InvalidOperationException("Stored options could not be deserialized.");

        var attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(request.Attestation.GetRawText())
            ?? throw new ArgumentException("attestation could not be parsed", nameof(request));

        var result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (args, _ct) =>
            {
                var credId = Base64Url.EncodeToString(args.CredentialId);
                var existing = await credentials.FindByCredentialId(credId, _ct);
                return existing is null;
            },
        }, ct);

        var credentialIdBase64 = Base64Url.EncodeToString(result.Id);

        var cred = new WebAuthnCredential(
            Id: Guid.NewGuid(),
            IdentityId: session.IdentityId,
            CredentialId: credentialIdBase64,
            PublicKey: result.PublicKey,
            Algorithm: 0,  // The COSE algorithm id lives inside PublicKey; decode lazily when needed.
            Aaguid: result.AaGuid == Guid.Empty ? null : result.AaGuid,
            Nickname: string.IsNullOrWhiteSpace(request.Nickname) ? null : request.Nickname,
            SignCount: result.SignCount,
            CreatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null,
            RevokedAt: null);

        await credentials.Collection.InsertOneAsync(cred, cancellationToken: ct);
        return Results.Created($"/v1/auth/credentials/webauthn/{cred.Id}", cred);
    }
}
