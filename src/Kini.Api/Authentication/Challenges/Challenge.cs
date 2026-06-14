namespace Kini.Api.Authentication.Challenges;

public enum ChallengeKind
{
    Ssh,
    WebAuthnAssertion,
    WebAuthnRegistration,
}

public sealed record Challenge(
    Guid Id,
    ChallengeKind Kind,
    string? Email,           // SSH flow: the email being asserted
    Guid? IdentityId,        // WebAuthn registration: the caller is already known
    string Payload,          // SSH: the nonce string. WebAuthn: serialized options JSON.
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt);
