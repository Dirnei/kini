using System.Security.Cryptography;
using System.Text;
using Kini.Api.Authentication.Identities;
using Kini.Api.Keys;
using Kini.Api.Organizations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Kini.Api.WellKnown;

public static class WellKnownEndpoints
{
    public static IEndpointRouteBuilder MapWellKnownEndpoints(this IEndpointRouteBuilder app)
    {
        // Public, unauthenticated, byte-shaped responses.
        //
        // Three URL shapes for the same key data:
        //   /{user}.keys                       flat, lookup by globally-unique username
        //   /{org}/{user}.keys                 org-scoped; {org} is the org's slug OR primaryDomain
        //   /.well-known/openpgpkey/...        WKD (GPG, RFC draft-koch-openpgp-webkey-service)
        app.MapGet("/{user}.keys", GetUserDotKeys).WithTags("well-known");
        app.MapGet("/{user}.gpg",  GetUserDotGpg).WithTags("well-known");

        app.MapGet("/{org}/{user}.keys", GetOrgScopedDotKeys).WithTags("well-known");
        app.MapGet("/{org}/{user}.gpg",  GetOrgScopedDotGpg).WithTags("well-known");

        app.MapGet("/.well-known/openpgpkey/hu/{localHash}", GetWkdDirect).WithTags("well-known");
        app.MapGet("/.well-known/openpgpkey/{domain}/hu/{localHash}", GetWkdAdvanced).WithTags("well-known");
        return app;
    }

    // ── flat /{user}.keys / .gpg ─────────────────────────────────────────

    private static async Task<IResult> GetUserDotKeys(
        string user, IdentitiesCollection identities, KeysCollection keys, CancellationToken ct)
    {
        var identity = await identities.FindByUsername(user.Trim().ToLowerInvariant(), ct);
        return await SshKeysFor(identity, keys, ct);
    }

    private static async Task<IResult> GetUserDotGpg(
        string user, IdentitiesCollection identities, KeysCollection keys, CancellationToken ct)
    {
        var identity = await identities.FindByUsername(user.Trim().ToLowerInvariant(), ct);
        return await GpgKeysFor(identity, keys, ct);
    }

    // ── org-scoped /{org}/{user}.keys / .gpg ─────────────────────────────

    private static async Task<IResult> GetOrgScopedDotKeys(
        string org, string user,
        OrganizationsCollection orgs, IdentitiesCollection identities, KeysCollection keys,
        CancellationToken ct)
    {
        var identity = await ResolveOrgScoped(org, user, orgs, identities, ct);
        return await SshKeysFor(identity, keys, ct);
    }

    private static async Task<IResult> GetOrgScopedDotGpg(
        string org, string user,
        OrganizationsCollection orgs, IdentitiesCollection identities, KeysCollection keys,
        CancellationToken ct)
    {
        var identity = await ResolveOrgScoped(org, user, orgs, identities, ct);
        return await GpgKeysFor(identity, keys, ct);
    }

    /// <summary>
    /// {org} can be either the org's primaryDomain (DNS-shaped, dot-containing)
    /// or its slug (alnum-hyphen, no dots). Slugs forbid dots so the two
    /// namespaces are disjoint — a single lookup attempt is enough.
    /// </summary>
    private static async Task<Identity?> ResolveOrgScoped(
        string org, string user,
        OrganizationsCollection orgs, IdentitiesCollection identities,
        CancellationToken ct)
    {
        var key = org.Trim().ToLowerInvariant();
        Organization? matched = key.Contains('.')
            ? await orgs.FindByPrimaryDomain(key, ct)
            : await orgs.FindBySlug(key, ct);
        if (matched is null) return null;

        var identity = await identities.FindByUsername(user.Trim().ToLowerInvariant(), ct);
        if (identity is null || identity.OrgId != matched.Id) return null;
        return identity;
    }

    // ── shared shapers ───────────────────────────────────────────────────

    private static async Task<IResult> SshKeysFor(Identity? identity, KeysCollection keys, CancellationToken ct)
    {
        if (identity is null) return Results.NotFound();
        var docs = await keys.ActiveForIdentity(identity.Id, type: "ssh", ct);
        var body = string.Join('\n', docs.Select(k => k.PublicKey)) + (docs.Count > 0 ? "\n" : "");
        return Results.Text(body, "text/plain; charset=utf-8");
    }

    private static async Task<IResult> GpgKeysFor(Identity? identity, KeysCollection keys, CancellationToken ct)
    {
        if (identity is null) return Results.NotFound();
        var docs = await keys.ActiveForIdentity(identity.Id, type: "gpg", ct);
        if (docs.Count == 0) return Results.NotFound();
        var body = string.Join('\n', docs.Select(k => k.PublicKey.TrimEnd())) + '\n';
        return Results.Text(body, "application/pgp-keys");
    }

    // ── WKD ──────────────────────────────────────────────────────────────

    private static async Task<IResult> GetWkdDirect(
        string localHash, HttpContext http,
        IdentitiesCollection identities, KeysCollection keys, CancellationToken ct)
    {
        var domain = http.Request.Host.Host;
        return await ServeWkd(domain, localHash, identities, keys, ct);
    }

    private static Task<IResult> GetWkdAdvanced(
        string domain, string localHash,
        IdentitiesCollection identities, KeysCollection keys, CancellationToken ct) =>
        ServeWkd(domain, localHash, identities, keys, ct);

    private static async Task<IResult> ServeWkd(
        string domain, string localHash,
        IdentitiesCollection identities, KeysCollection keys, CancellationToken ct)
    {
        var isDev = IsLocalHost(domain);
        var filter = isDev
            ? Builders<Identity>.Filter.Empty
            : Builders<Identity>.Filter.Regex(i => i.Email,
                new BsonRegularExpression($"@{System.Text.RegularExpressions.Regex.Escape(domain)}$", "i"));

        using var cursor = await identities.Collection.FindAsync(filter, cancellationToken: ct);
        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var i in cursor.Current)
            {
                var localPart = i.Email.Split('@', 2)[0];
                if (WkdHashFor(localPart) != localHash) continue;

                var docs = await keys.ActiveForIdentity(i.Id, type: "gpg", ct);
                if (docs.Count == 0) return Results.NotFound();

                using var ms = new MemoryStream();
                foreach (var k in docs)
                {
                    try
                    {
                        var bin = GpgPublicKey.Dearmor(k.PublicKey);
                        ms.Write(bin, 0, bin.Length);
                    }
                    catch (FormatException) { /* skip malformed */ }
                }
                if (ms.Length == 0) return Results.NotFound();

                return Results.Bytes(ms.ToArray(), "application/octet-stream");
            }
        }
        return Results.NotFound();
    }

    /// <summary>SHA-1(lowercased local-part) → z-base-32. Per draft-koch-openpgp-webkey-service.</summary>
    public static string WkdHashFor(string localPart)
    {
        var lower = localPart.ToLowerInvariant();
        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(Encoding.UTF8.GetBytes(lower), hash);
        return ZBase32.Encode(hash);
    }

    private static bool IsLocalHost(string host) =>
        host is "localhost" or "127.0.0.1" or "::1" || host.EndsWith(".localhost");
}
