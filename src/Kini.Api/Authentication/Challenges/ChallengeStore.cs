using System.Security.Cryptography;
using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.Authentication.Challenges;

/// <summary>
/// Thin orchestrator around <see cref="ChallengesCollection"/>: issue a new
/// challenge, consume one (mark used + verify it's still valid).
/// </summary>
public sealed class ChallengeStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);

    private readonly ChallengesCollection _challenges;

    public ChallengeStore(ChallengesCollection challenges)
    {
        _challenges = challenges;
    }

    public async Task<Challenge> Issue(
        ChallengeKind kind,
        string? email,
        Guid? identityId,
        string payload,
        TimeSpan? lifetime = null,
        CancellationToken ct = default)
    {
        var challenge = new Challenge(
            Id: Guid.NewGuid(),
            Kind: kind,
            Email: email?.Trim().ToLowerInvariant(),
            IdentityId: identityId,
            Payload: payload,
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.Add(lifetime ?? DefaultLifetime),
            ConsumedAt: null);

        await _challenges.Collection.InsertOneAsync(challenge, cancellationToken: ct);
        return challenge;
    }

    /// <summary>Generates a random nonce string suitable for SSH-key signing payloads.</summary>
    public static string GenerateNonce()
    {
        Span<byte> raw = stackalloc byte[24];
        RandomNumberGenerator.Fill(raw);
        return Base64UrlEncoder.Encode(raw);
    }

    /// <summary>
    /// Atomically consumes a challenge by id+kind+payload. Returns null if the challenge
    /// doesn't exist, is expired, already used, or doesn't match the supplied criteria.
    /// </summary>
    public async Task<Challenge?> Consume(
        ChallengeKind kind,
        Guid challengeId,
        CancellationToken ct = default)
    {
        var filter = Builders<Challenge>.Filter.And(
            Builders<Challenge>.Filter.Eq(c => c.Id, challengeId),
            Builders<Challenge>.Filter.Eq(c => c.Kind, kind),
            Builders<Challenge>.Filter.Eq(c => c.ConsumedAt, null),
            Builders<Challenge>.Filter.Gt(c => c.ExpiresAt, DateTimeOffset.UtcNow));

        var update = Builders<Challenge>.Update.Set(c => c.ConsumedAt, DateTimeOffset.UtcNow);

        return await _challenges.Collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<Challenge> { ReturnDocument = ReturnDocument.After },
            ct);
    }

    /// <summary>
    /// Consume by email+kind, returning the matching challenge. Used by the SSH flow
    /// where the client posts back the nonce string rather than a challenge id.
    /// </summary>
    public async Task<Challenge?> ConsumeByEmailAndNonce(
        ChallengeKind kind,
        string email,
        string nonce,
        CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var filter = Builders<Challenge>.Filter.And(
            Builders<Challenge>.Filter.Eq(c => c.Kind, kind),
            Builders<Challenge>.Filter.Eq(c => c.Email, normalizedEmail),
            Builders<Challenge>.Filter.Eq(c => c.Payload, nonce),
            Builders<Challenge>.Filter.Eq(c => c.ConsumedAt, null),
            Builders<Challenge>.Filter.Gt(c => c.ExpiresAt, DateTimeOffset.UtcNow));

        var update = Builders<Challenge>.Update.Set(c => c.ConsumedAt, DateTimeOffset.UtcNow);

        return await _challenges.Collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<Challenge> { ReturnDocument = ReturnDocument.After },
            ct);
    }
}
