using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Authentication.Identities;

public static class CurrentIdentityExtensions
{
    /// <summary>Fetches the Identity record corresponding to the current session.</summary>
    public static Task<Identity?> GetCurrentIdentity(
        this HttpContext http,
        IdentitiesCollection identities,
        CancellationToken ct = default)
    {
        var session = http.GetSession();
        return identities.FindById(session.IdentityId, ct);
    }
}
