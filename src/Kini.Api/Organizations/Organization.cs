namespace Kini.Api.Organizations;

public sealed record Organization(
    Guid Id,
    string Name,
    string? PrimaryDomain,
    DateTimeOffset CreatedAt);
