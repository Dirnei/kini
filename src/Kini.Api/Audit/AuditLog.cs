using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Audit;

/// <summary>
/// Thin write-side helper. Callers don't construct AuditEvent directly — they
/// hand us an Action + target + detail and we stamp the actor from the
/// HttpContext + identity lookup.
/// </summary>
public sealed class AuditLog
{
    private readonly AuditCollection _audit;
    private readonly IdentitiesCollection _identities;
    private readonly ILogger<AuditLog> _log;

    public AuditLog(AuditCollection audit, IdentitiesCollection identities, ILogger<AuditLog> log)
    {
        _audit = audit;
        _identities = identities;
        _log = log;
    }

    public Task RecordAs(
        Guid orgId,
        AuditActor actor,
        string action,
        AuditTarget? target = null,
        Dictionary<string, string>? detail = null,
        CancellationToken ct = default)
    {
        var evt = new AuditEvent(
            Id: Guid.NewGuid(),
            OrgId: orgId,
            Action: action,
            Actor: actor,
            Target: target,
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: detail);

        // Audit writes never throw out to the caller — observability mustn't
        // break the request it's observing. Worst case we log and move on.
        return SafeInsert(evt, ct);
    }

    public async Task Record(
        HttpContext http,
        string action,
        AuditTarget? target = null,
        Dictionary<string, string>? detail = null,
        CancellationToken ct = default)
    {
        var session = http.GetSessionOrNull();
        AuditActor actor;
        Guid orgId;

        if (session is not null)
        {
            var caller = await _identities.FindById(session.IdentityId, ct);
            actor = new AuditActor("user", session.IdentityId, caller?.Email);
            orgId = session.OrgId;
        }
        else
        {
            actor = new AuditActor("anonymous", null, null);
            // For anonymous audit events (e.g., sign-up), the caller must supply
            // the orgId via Detail["orgId"] — handled by the static convenience
            // method below.
            if (detail is null || !detail.TryGetValue("orgId", out var s) || !Guid.TryParse(s, out orgId))
            {
                _log.LogWarning("Anonymous audit event {Action} dropped — no orgId provided", action);
                return;
            }
            detail = new Dictionary<string, string>(detail);
            detail.Remove("orgId");
        }

        var evt = new AuditEvent(
            Id: Guid.NewGuid(),
            OrgId: orgId,
            Action: action,
            Actor: actor,
            Target: target,
            OccurredAt: DateTimeOffset.UtcNow,
            Detail: detail);

        await SafeInsert(evt, ct);
    }

    private async Task SafeInsert(AuditEvent evt, CancellationToken ct)
    {
        try { await _audit.Collection.InsertOneAsync(evt, cancellationToken: ct); }
        catch (Exception ex) { _log.LogWarning(ex, "Audit insert failed for {Action}", evt.Action); }
    }
}
