namespace Kini.Api.Keys;

/// <summary>
/// A published public key in the directory. SSH or GPG — the polymorphism
/// is flattened at storage level (single record with nullable per-type
/// fields) and re-projected to the spec's discriminated shape at the API.
///
/// Distinct from <see cref="Kini.Api.Authentication.Credentials.SshCredential"/>
/// which is "how this identity authenticates." A published Key is "what this
/// identity vouches for to the world."
/// </summary>
public sealed record Key(
    Guid Id,
    Guid IdentityId,
    Guid OrgId,
    string Type,             // "ssh" | "gpg"
    string Fingerprint,
    string Algorithm,
    string PublicKey,        // canonical authorized_keys line (ssh) or armored block (gpg)
    string? Comment,         // ssh trailing comment
    string[]? Uids,          // gpg user ids
    string Provenance,       // uploaded | binary | binary_hardware | binary_attested
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? RevocationReason);
