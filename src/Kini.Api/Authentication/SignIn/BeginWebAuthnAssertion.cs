using System.Buffers.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;

namespace Kini.Api.Authentication.SignIn;

public static class BeginWebAuthnAssertion
{
    public sealed record Request(string Email);

    public static async Task<IResult> Handle(
        Request request,
        IFido2 fido2,
        IdentitiesCollection identities,
        WebAuthnCredentialsCollection webauthn,
        ChallengeStore challenges,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { code = "invalid_email" });

        var identity = await identities.FindByEmail(request.Email, ct);
        var allowedCreds = identity is null
            ? new List<PublicKeyCredentialDescriptor>()
            : (await webauthn.ActiveForIdentity(identity.Id, ct))
                .Select(c => new PublicKeyCredentialDescriptor(Base64Url.DecodeFromChars(c.CredentialId)))
                .ToList();

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCreds,
            UserVerification = UserVerificationRequirement.Preferred,
        });

        var challenge = await challenges.Issue(
            kind: ChallengeKind.WebAuthnAssertion,
            email: request.Email,
            identityId: identity?.Id,
            payload: JsonSerializer.Serialize(options),
            ct: ct);

        return Results.Ok(new { ceremonyId = challenge.Id.ToString(), options });
    }
}
