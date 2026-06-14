namespace Kini.Api.ApiTokens;

public sealed record ApiToken(
    Guid Id,
    Guid IdentityId,
    Guid OrgId,
    string Name,
    string TokenHash,             // SHA-256 of the plaintext token (URL-safe base64)
    string[] Scopes,              // free-form for v0; enforced later
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);
