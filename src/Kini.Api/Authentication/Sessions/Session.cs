using System.Text.Json.Serialization;

namespace Kini.Api.Authentication.Sessions;

public sealed record Session(
    Guid Id,
    Guid IdentityId,
    Guid OrgId,
    // TokenHash is the lookup key — never expose it on the HTTP boundary.
    // System.Text.Json honors [property: JsonIgnore] on record positional
    // parameters; MongoDB.Bson serialization uses its own attributes and is
    // unaffected, so persistence still round-trips this field.
    [property: JsonIgnore] string TokenHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt);
