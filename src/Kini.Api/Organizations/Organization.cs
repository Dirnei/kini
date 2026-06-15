namespace Kini.Api.Organizations;

public sealed record Organization(
    Guid Id,
    string Name,
    string Slug,                   // required + globally unique. Lowercase alnum-hyphen, no dots.
    string? PrimaryDomain,         // optional + globally unique when set. DNS-style.
    DateTimeOffset CreatedAt);
