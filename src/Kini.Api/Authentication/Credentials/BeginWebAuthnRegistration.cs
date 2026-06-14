using System.Buffers.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Authentication.Credentials;

public static class BeginWebAuthnRegistration
{
    public static async Task<IResult> Handle(
        HttpContext http,
        IFido2 fido2,
        IdentitiesCollection identities,
        WebAuthnCredentialsCollection webauthn,
        ChallengeStore challenges,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var identity = await identities.FindById(session.IdentityId, ct);
        if (identity is null) return Results.Unauthorized();

        // Exclude credentials already bound to this identity, so the user can't
        // accidentally re-register the same authenticator twice.
        var existing = await webauthn.ActiveForIdentity(identity.Id, ct);
        var excludeList = existing
            .Select(c => new PublicKeyCredentialDescriptor(Base64Url.DecodeFromChars(c.CredentialId)))
            .ToList();

        var user = new Fido2User
        {
            Id = identity.Id.ToByteArray(),
            Name = identity.Email,
            DisplayName = identity.DisplayName ?? identity.Email,
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = user,
            ExcludeCredentials = excludeList,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
        });

        var challenge = await challenges.Issue(
            kind: ChallengeKind.WebAuthnRegistration,
            email: identity.Email,
            identityId: identity.Id,
            payload: JsonSerializer.Serialize(options),
            ct: ct);

        return Results.Ok(new { ceremonyId = challenge.Id.ToString(), options });
    }
}
