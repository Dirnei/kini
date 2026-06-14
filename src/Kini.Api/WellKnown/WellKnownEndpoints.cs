using System.Security.Cryptography;
using System.Text;
using Kini.Api.Authentication.Identities;
using Kini.Api.Keys;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Kini.Api.WellKnown;

public static class WellKnownEndpoints
{
    public static IEndpointRouteBuilder MapWellKnownEndpoints(this IEndpointRouteBuilder app)
    {
        // Public, unauthenticated, byte-shaped responses.
        app.MapGet("/{user}.keys", GetUserDotKeys).WithTags("well-known");
        app.MapGet("/{user}.gpg", GetUserDotGpg).WithTags("well-known");
        app.MapGet("/.well-known/openpgpkey/hu/{localHash}", GetWkdDirect).WithTags("well-known");
        app.MapGet("/.well-known/openpgpkey/{domain}/hu/{localHash}", GetWkdAdvanced).WithTags("well-known");
        return app;
    }

    /// <summary>
    /// GitHub-style SSH keys endpoint. One authorized_keys-format line per
    /// active SSH key, newline-separated.
    /// </summary>
    private static async Task<IResult> GetUserDotKeys(
        string user,
        HttpContext http,
        IdentitiesCollection identities,
        KeysCollection keys,
        CancellationToken ct)
    {
        var identity = await ResolveByUsername(user, identities, ct);
        if (identity is null) return Results.NotFound();

        var docs = await keys.ActiveForIdentity(identity.Id, type: "ssh", ct);
        var body = string.Join('\n', docs.Select(k => k.PublicKey)) + (docs.Count > 0 ? "\n" : "");
        return Results.Text(body, "text/plain; charset=utf-8");
    }

    /// <summary>
    /// GitHub-style GPG endpoint. Returns the ASCII-armored OpenPGP public key
    /// (concatenated if the identity has multiple active GPG keys).
    /// </summary>
    private static async Task<IResult> GetUserDotGpg(
        string user,
        IdentitiesCollection identities,
        KeysCollection keys,
        CancellationToken ct)
    {
        var identity = await ResolveByUsername(user, identities, ct);
        if (identity is null) return Results.NotFound();

        var docs = await keys.ActiveForIdentity(identity.Id, type: "gpg", ct);
        if (docs.Count == 0) return Results.NotFound();

        var body = string.Join('\n', docs.Select(k => k.PublicKey.TrimEnd())) + '\n';
        return Results.Text(body, "application/pgp-keys");
    }

    /// <summary>
    /// WKD direct method: <c>https://&lt;domain&gt;/.well-known/openpgpkey/hu/&lt;hash&gt;</c>
    /// Returns the binary OpenPGP transferable public key.
    /// </summary>
    private static async Task<IResult> GetWkdDirect(
        string localHash,
        HttpContext http,
        IdentitiesCollection identities,
        KeysCollection keys,
        CancellationToken ct)
    {
        var domain = http.Request.Host.Host;
        return await ServeWkd(domain, localHash, identities, keys, ct);
    }

    /// <summary>
    /// WKD advanced method: served from <c>openpgpkey.&lt;domain&gt;</c> with
    /// the domain repeated in the URL path. Same response shape as direct.
    /// </summary>
    private static Task<IResult> GetWkdAdvanced(
        string domain,
        string localHash,
        IdentitiesCollection identities,
        KeysCollection keys,
        CancellationToken ct) =>
        ServeWkd(domain, localHash, identities, keys, ct);

    private static async Task<IResult> ServeWkd(
        string domain,
        string localHash,
        IdentitiesCollection identities,
        KeysCollection keys,
        CancellationToken ct)
    {
        // For each identity whose email matches @domain (or any identity in dev
        // localhost mode), compute its WKD hash and compare. Costly only when
        // the identity count grows; v0 acceptable.
        var isDev = IsLocalHost(domain);
        var filter = isDev
            ? Builders<Identity>.Filter.Empty
            : Builders<Identity>.Filter.Regex(i => i.Email, new BsonRegularExpression($"@{System.Text.RegularExpressions.Regex.Escape(domain)}$", "i"));

        using var cursor = await identities.Collection.FindAsync(filter, cancellationToken: ct);
        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var i in cursor.Current)
            {
                var localPart = i.Email.Split('@', 2)[0];
                if (WkdHashFor(localPart) != localHash) continue;

                var docs = await keys.ActiveForIdentity(i.Id, type: "gpg", ct);
                if (docs.Count == 0) return Results.NotFound();

                // Concatenate binary public-key packet streams from each active GPG key.
                // gpg --locate-keys accepts a single packet stream that contains multiple
                // keys.
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

    /// <summary>SHA-1(lowercased local-part) → z-base-32. RFC: draft-koch-openpgp-webkey-service.</summary>
    public static string WkdHashFor(string localPart)
    {
        var lower = localPart.ToLowerInvariant();
        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(Encoding.UTF8.GetBytes(lower), hash);
        return ZBase32.Encode(hash);
    }

    private static bool IsLocalHost(string host) =>
        host is "localhost" or "127.0.0.1" or "::1" || host.EndsWith(".localhost");

    private static Task<Identity?> ResolveByUsername(
        string user,
        IdentitiesCollection identities,
        CancellationToken ct) =>
        identities.FindByUsername(user.Trim().ToLowerInvariant(), ct);
}
