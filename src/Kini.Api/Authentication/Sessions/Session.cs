namespace Kini.Api.Authentication.Sessions;

public sealed record Session(
    Guid Id,
    Guid IdentityId,
    Guid OrgId,
    string TokenHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt);
