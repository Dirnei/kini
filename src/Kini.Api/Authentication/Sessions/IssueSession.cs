using Kini.Api.Authentication.Identities;

namespace Kini.Api.Authentication.Sessions;

public sealed class IssueSession
{
    private readonly SessionsCollection _sessions;
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public IssueSession(SessionsCollection sessions)
    {
        _sessions = sessions;
    }

    public async Task<(Session Session, string PlaintextToken)> ForIdentity(Identity identity, CancellationToken ct = default)
    {
        var (plaintext, hash) = TokenGenerator.Issue();
        var session = new Session(
            Id: Guid.NewGuid(),
            IdentityId: identity.Id,
            OrgId: identity.OrgId,
            TokenHash: hash,
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.Add(Lifetime),
            RevokedAt: null);

        await _sessions.Collection.InsertOneAsync(session, cancellationToken: ct);
        return (session, plaintext);
    }
}
