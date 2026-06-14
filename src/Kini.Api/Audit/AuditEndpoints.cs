using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Audit;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/audit", ListAuditEvents.Handle)
           .WithTags("audit")
           .RequireSession();
        return app;
    }
}
