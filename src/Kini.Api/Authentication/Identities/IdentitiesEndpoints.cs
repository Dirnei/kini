using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Authentication.Identities;

public static class IdentitiesEndpoints
{
    public static IEndpointRouteBuilder MapIdentitiesEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/orgs/{orgId:guid}/identities").WithTags("identities");
        grp.MapGet("/", ListIdentities.Handle).RequireSession();
        grp.MapPost("/", CreateIdentity.Handle).RequireSession();
        return app;
    }
}
