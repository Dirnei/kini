using Kini.Api.ApiTokens;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;

namespace Kini.Api.Authentication.Sessions;

public sealed class BearerAuthMiddleware
{
    private const string SessionItemKey = "Kini.Session";

    private readonly RequestDelegate _next;

    public BearerAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SessionsCollection sessions, ApiTokensCollection apiTokens)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out StringValues auth) &&
            auth.Count > 0 &&
            auth[0]!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth[0]!["Bearer ".Length..].Trim();
            if (token.Length > 0)
            {
                var hash = TokenGenerator.HashOf(token);

                // Try interactive sessions first (the common case from the web UI).
                var session = await sessions.FindByTokenHash(hash, context.RequestAborted);
                if (session is not null &&
                    session.RevokedAt is null &&
                    session.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    context.Items[SessionItemKey] = session;
                }
                else
                {
                    // Fall back to API tokens — same Authorization: Bearer shape, different
                    // collection. Looks like a session to downstream code (so RequireSession
                    // / GetSession just work), but identity-stamped from the ApiToken.
                    var apiToken = await apiTokens.FindByTokenHash(hash, context.RequestAborted);
                    if (apiToken is not null && apiToken.RevokedAt is null)
                    {
                        var pseudoSession = new Session(
                            Id: apiToken.Id,                 // not a real session id; identifies the token
                            IdentityId: apiToken.IdentityId,
                            OrgId: apiToken.OrgId,
                            TokenHash: apiToken.TokenHash,
                            CreatedAt: apiToken.CreatedAt,
                            ExpiresAt: DateTimeOffset.MaxValue,
                            RevokedAt: null);
                        context.Items[SessionItemKey] = pseudoSession;

                        // Touch lastUsedAt; fire-and-forget.
                        _ = apiTokens.Collection.UpdateOneAsync(
                            t => t.Id == apiToken.Id,
                            Builders<ApiToken>.Update.Set(t => t.LastUsedAt, DateTimeOffset.UtcNow),
                            cancellationToken: context.RequestAborted);
                    }
                }
            }
        }

        await _next(context);
    }

    internal static Session? TryGetSession(HttpContext http) =>
        http.Items.TryGetValue(SessionItemKey, out var v) ? v as Session : null;
}

public static class SessionAccessExtensions
{
    /// <summary>Resolves the current session or throws — for endpoints behind RequireSession.</summary>
    public static Session GetSession(this HttpContext http) =>
        BearerAuthMiddleware.TryGetSession(http)
            ?? throw new InvalidOperationException("No session on this request. Did you forget RequireSession()?");

    /// <summary>Resolves the current session if any, without throwing.</summary>
    public static Session? GetSessionOrNull(this HttpContext http) =>
        BearerAuthMiddleware.TryGetSession(http);

    /// <summary>Endpoint filter — 401 if there's no session, run the endpoint otherwise.</summary>
    public static TBuilder RequireSession<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(async (ctx, next) =>
        {
            if (BearerAuthMiddleware.TryGetSession(ctx.HttpContext) is null)
                return Results.Unauthorized();
            return await next(ctx);
        });
        return builder;
    }
}
