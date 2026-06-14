using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;

namespace Kini.Api.Authentication.SignIn;

public static class IssueSshChallenge
{
    public sealed record Request(string Email);

    public sealed record Response(string Nonce, string Namespace, DateTimeOffset ExpiresAt);

    public static async Task<IResult> Handle(
        Request request,
        IdentitiesCollection identities,
        SshCredentialsCollection credentials,
        ChallengeStore challenges,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { code = "invalid_email" });

        // Issue the challenge unconditionally to avoid email enumeration. If the
        // email has no identity or no live SSH credential, /verify will still
        // refuse — the attacker just learns "maybe yes, maybe no."
        var identity = await identities.FindByEmail(request.Email, ct);
        if (identity is not null)
        {
            var live = await credentials.ActiveForIdentity(identity.Id, ct);
            if (live.Count == 0)
            {
                // Still issue a (decoy) challenge so timing/structure doesn't differ.
            }
        }

        var nonce = ChallengeStore.GenerateNonce();
        var challenge = await challenges.Issue(
            kind: ChallengeKind.Ssh,
            email: request.Email,
            identityId: identity?.Id,
            payload: nonce,
            ct: ct);

        return Results.Ok(new Response(nonce, SshSignatureVerifier.Namespace, challenge.ExpiresAt));
    }
}
