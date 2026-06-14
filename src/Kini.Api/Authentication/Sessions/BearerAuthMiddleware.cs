using Microsoft.Extensions.Primitives;

namespace Kini.Api.Authentication.Sessions;

public sealed class BearerAuthMiddleware
{
    private const string SessionItemKey = "Kini.Session";

    private readonly RequestDelegate _next;

    public BearerAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SessionsCollection sessions)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out StringValues auth) &&
            auth.Count > 0 &&
            auth[0]!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth[0]!["Bearer ".Length..].Trim();
            if (token.Length > 0)
            {
                var hash = TokenGenerator.HashOf(token);
                var session = await sessions.FindByTokenHash(hash, context.RequestAborted);

                if (session is not null &&
                    session.RevokedAt is null &&
                    session.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    context.Items[SessionItemKey] = session;
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
