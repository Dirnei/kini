namespace Kini.Api.Authentication.Credentials;

public sealed record SshCredential(
    Guid Id,
    Guid IdentityId,
    string PublicKey,         // single authorized_keys-format line
    string Fingerprint,       // "SHA256:..."
    string Algorithm,         // e.g. ssh-ed25519, ssh-rsa
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);
