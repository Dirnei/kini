namespace Kini.Api.Audit;

/// <summary>
/// Append-only record of important state changes. Org-scoped so admins of
/// one org never see another org's events.
/// </summary>
public sealed record AuditEvent(
    Guid Id,
    Guid OrgId,
    string Action,                // dotted verb: "key.published", "identity.created", etc.
    AuditActor Actor,
    AuditTarget? Target,
    DateTimeOffset OccurredAt,
    Dictionary<string, string>? Detail);

public sealed record AuditActor(
    string Type,                  // "user" | "api-token" | "system" | "anonymous"
    Guid? IdentityId,
    string? Email);

public sealed record AuditTarget(
    string Type,                  // "key", "identity", "credential", "session", "api-token", "org"
    Guid? Id,
    string? Name);                // free-form label for the UI (fingerprint, email, etc.)

public static class AuditAction
{
    public const string OrgCreated            = "org.created";
    public const string IdentityCreated       = "identity.created";
    public const string KeyPublished          = "key.published";
    public const string KeyRevoked            = "key.revoked";
    public const string KeyDeleted            = "key.deleted";
    public const string CredentialRegistered  = "credential.registered";
    public const string CredentialRevoked     = "credential.revoked";
    public const string SignInSucceeded       = "signin.succeeded";
    public const string SignInFailed          = "signin.failed";
    public const string SessionRevoked        = "session.revoked";
    public const string ApiTokenCreated       = "apitoken.created";
    public const string ApiTokenRevoked       = "apitoken.revoked";
}
