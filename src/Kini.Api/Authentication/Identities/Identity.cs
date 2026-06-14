namespace Kini.Api.Authentication.Identities;

public sealed record Identity(
    Guid Id,
    Guid OrgId,
    string Username,           // globally unique public handle (URL identifier)
    string Email,              // globally unique; used by WKD and login forms
    string Role,               // see IdentityRole — "owner" or "member"
    string? DisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? VerifiedAt);
