namespace Kini.Api.Organizations;

public static class OrganizationsEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/orgs").WithTags("orgs");

        group.MapPost("/", CreateOrganization.Handle);
        group.MapGet("/{orgId:guid}", GetOrganization.Handle);

        return app;
    }
}
