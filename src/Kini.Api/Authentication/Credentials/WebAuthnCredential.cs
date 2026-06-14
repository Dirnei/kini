namespace Kini.Api.Authentication.Credentials;

public sealed record WebAuthnCredential(
    Guid Id,
    Guid IdentityId,
    string CredentialId,      // base64url
    byte[] PublicKey,         // COSE-encoded public key blob
    int Algorithm,            // COSE algorithm identifier (e.g. -7 for ES256, -8 for EdDSA)
    Guid? Aaguid,             // authenticator model identifier
    string? Nickname,
    long SignCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);
