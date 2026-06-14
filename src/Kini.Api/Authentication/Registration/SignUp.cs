using Kini.Api.Audit;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;
using Kini.Api.Keys;
using Kini.Api.Organizations;
using MongoDB.Driver;
using IdentityRole = Kini.Api.Authentication.Identities.IdentityRole;

namespace Kini.Api.Authentication.Registration;

public static class SignUp
{
    public sealed record Request(
        string OrganizationName,
        string? PrimaryDomain,
        string Username,
        string Email,
        string? DisplayName,
        string SshPublicKey,
        bool? PublishKey);    // null/true → publish; false → keep private to auth

    /// <summary>
    /// Anonymous bootstrap. Creates an Organization, Identity, initial SshCredential,
    /// and a Session — all in one shot, returning the bearer token.
    /// </summary>
    public static async Task<IResult> Handle(
        Request request,
        HttpContext http,
        OrganizationsCollection orgs,
        IdentitiesCollection identities,
        SshCredentialsCollection sshCreds,
        KeysCollection keys,
        AuditLog audit,
        IssueSession issueSession,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return Results.BadRequest(new { code = "invalid_organization_name" });
        if (!Username.TryNormalize(request.Username, out var username, out var usernameError))
            return Results.BadRequest(new { code = "invalid_username", message = usernameError });
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return Results.BadRequest(new { code = "invalid_email" });

        // Parse & validate the SSH public key BEFORE writing anything else.
        SshPublicKey parsedKey;
        try { parsedKey = SshPublicKey.Parse(request.SshPublicKey); }
        catch (FormatException fx)
        {
            return Results.BadRequest(new { code = "invalid_public_key", message = fx.Message });
        }

        // Refuse if the email is already in use, BEFORE creating the org.
        // (No cross-document transaction here — for v0, just guarded by the
        // unique index on email; we catch the duplicate-key on identity insert.)
        var emailLower = request.Email.Trim().ToLowerInvariant();
        var existingIdentity = await identities.FindByEmail(emailLower, ct);
        if (existingIdentity is not null)
            return Results.Conflict(new { code = "email_taken" });

        var org = new Organization(
            Id: Guid.NewGuid(),
            Name: request.OrganizationName.Trim(),
            PrimaryDomain: string.IsNullOrWhiteSpace(request.PrimaryDomain) ? null : request.PrimaryDomain.Trim(),
            CreatedAt: DateTimeOffset.UtcNow);

        await orgs.Collection.InsertOneAsync(org, cancellationToken: ct);

        var identity = new Identity(
            Id: Guid.NewGuid(),
            OrgId: org.Id,
            Username: username,
            Email: emailLower,
            Role: IdentityRole.Owner,           // bootstrap user is the Owner
            DisplayName: request.DisplayName,
            CreatedAt: DateTimeOffset.UtcNow,
            VerifiedAt: DateTimeOffset.UtcNow); // self-verified by holding the SSH key

        try
        {
            await identities.Collection.InsertOneAsync(identity, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Email OR username collision — roll back the org and report which.
            await orgs.Collection.DeleteOneAsync(o => o.Id == org.Id, ct);
            var code = ex.WriteError.Message.Contains("username") ? "username_taken" : "email_taken";
            return Results.Conflict(new { code });
        }

        var sshCred = new SshCredential(
            Id: Guid.NewGuid(),
            IdentityId: identity.Id,
            PublicKey: parsedKey.Canonical,
            Fingerprint: parsedKey.Fingerprint,
            Algorithm: parsedKey.Algorithm,
            Comment: parsedKey.Comment,
            CreatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null,
            RevokedAt: null);
        await sshCreds.Collection.InsertOneAsync(sshCred, cancellationToken: ct);

        // Auto-promote: by default, the sign-up key is also published to the
        // directory so consumers can immediately resolve it via WKD / .keys.
        // Opt out with publishKey=false.
        Key? publishedKey = null;
        if (request.PublishKey != false)
        {
            publishedKey = new Key(
                Id: Guid.NewGuid(),
                IdentityId: identity.Id,
                OrgId: org.Id,
                Type: "ssh",
                Fingerprint: parsedKey.Fingerprint,
                Algorithm: parsedKey.Algorithm,
                PublicKey: parsedKey.Canonical,
                Comment: parsedKey.Comment,
                Uids: null,
                Provenance: "uploaded",
                CreatedAt: DateTimeOffset.UtcNow,
                ExpiresAt: null,
                RevokedAt: null,
                RevocationReason: null);
            await keys.Collection.InsertOneAsync(publishedKey, cancellationToken: ct);
        }

        var (session, plaintext) = await issueSession.ForIdentity(identity, ct);

        // Audit trail: org created → identity created → key published (if any).
        // Stamped against the brand-new identity since they ARE the actor.
        var actor = new AuditActor("user", identity.Id, identity.Email);
        await audit.RecordAs(org.Id, actor, AuditAction.OrgCreated,
            new AuditTarget("org", org.Id, org.Name), ct: ct);
        await audit.RecordAs(org.Id, actor, AuditAction.IdentityCreated,
            new AuditTarget("identity", identity.Id, identity.Username), ct: ct);
        if (publishedKey is not null)
        {
            await audit.RecordAs(org.Id, actor, AuditAction.KeyPublished,
                new AuditTarget("key", publishedKey.Id, publishedKey.Fingerprint), ct: ct);
        }

        return Results.Created($"/v1/orgs/{org.Id}", new
        {
            organization = org,
            identity,
            sshCredential = sshCred,
            publishedKey,
            session = new
            {
                session.Id,
                session.IdentityId,
                session.OrgId,
                session.CreatedAt,
                session.ExpiresAt,
                session.RevokedAt,
                token = plaintext,
            },
        });
    }
}
