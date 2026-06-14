using Kini.Api.Audit;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Keys;

public static class UploadKey
{
    public sealed record Request(
        string Type,               // "ssh" or "gpg"
        string PublicKey,
        string? Provenance,        // optional override; defaults to "uploaded"
        DateTimeOffset? ExpiresAt);

    public static async Task<IResult> Handle(
        string email,
        Request request,
        HttpContext http,
        IdentitiesCollection identities,
        KeysCollection keys,
        AuditLog audit,
        CancellationToken ct)
    {
        var session = http.GetSession();

        var target = await identities.FindByEmail(email.Trim().ToLowerInvariant(), ct);
        if (target is null || target.OrgId != session.OrgId)
            return Results.NotFound();

        // Self-upload is always allowed. Cross-identity upload requires Owner.
        if (target.Id != session.IdentityId)
        {
            var caller = await identities.FindById(session.IdentityId, ct);
            if (caller is null || caller.Role != IdentityRole.Owner)
                return Results.Forbid();
        }

        var isGpg = string.Equals(request.Type, "gpg", StringComparison.OrdinalIgnoreCase);
        var isSsh = string.Equals(request.Type, "ssh", StringComparison.OrdinalIgnoreCase);
        if (!isGpg && !isSsh)
            return Results.BadRequest(new { code = "invalid_type", message = "type must be 'ssh' or 'gpg'." });

        var provenance = request.Provenance ?? "uploaded";
        if (provenance is not ("uploaded" or "binary" or "binary_hardware" or "binary_attested"))
            return Results.BadRequest(new { code = "invalid_provenance" });

        Key key;
        string algorithm;
        if (isGpg)
        {
            GpgPublicKey gpg;
            try { gpg = await GpgPublicKey.ParseAsync(request.PublicKey, ct); }
            catch (FormatException fx)
            {
                return Results.BadRequest(new { code = "invalid_public_key", message = fx.Message });
            }
            key = new Key(
                Id: Guid.NewGuid(),
                IdentityId: target.Id,
                OrgId: target.OrgId,
                Type: "gpg",
                Fingerprint: gpg.Fingerprint,
                Algorithm: gpg.Algorithm,
                PublicKey: gpg.Armored,
                Comment: null,
                Uids: gpg.Uids,
                Provenance: provenance,
                CreatedAt: DateTimeOffset.UtcNow,
                ExpiresAt: request.ExpiresAt,
                RevokedAt: null,
                RevocationReason: null);
            algorithm = gpg.Algorithm;
        }
        else
        {
            SshPublicKey parsed;
            try { parsed = SshPublicKey.Parse(request.PublicKey); }
            catch (FormatException fx)
            {
                return Results.BadRequest(new { code = "invalid_public_key", message = fx.Message });
            }
            key = new Key(
                Id: Guid.NewGuid(),
                IdentityId: target.Id,
                OrgId: target.OrgId,
                Type: "ssh",
                Fingerprint: parsed.Fingerprint,
                Algorithm: parsed.Algorithm,
                PublicKey: parsed.Canonical,
                Comment: parsed.Comment,
                Uids: null,
                Provenance: provenance,
                CreatedAt: DateTimeOffset.UtcNow,
                ExpiresAt: request.ExpiresAt,
                RevokedAt: null,
                RevocationReason: null);
            algorithm = parsed.Algorithm;
        }

        try
        {
            await keys.Collection.InsertOneAsync(key, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Results.Conflict(new { code = "already_published", message = "You've already published this key." });
        }

        await audit.Record(http, AuditAction.KeyPublished,
            new AuditTarget("key", key.Id, key.Fingerprint),
            new Dictionary<string, string>
            {
                ["targetIdentityId"] = target.Id.ToString(),
                ["targetEmail"] = target.Email,
                ["algorithm"] = algorithm,
                ["type"] = key.Type,
            }, ct);

        return Results.Created($"/v1/keys/{key.Id}", key);
    }
}
