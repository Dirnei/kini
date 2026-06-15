using Akka.Actor;
using Akka.Hosting;

namespace Kini.Api.Organizations;

public static class CreateOrganization
{
    public sealed record Request(string Name, string Slug, string? PrimaryDomain);

    public static async Task<IResult> Handle(
        Request request,
        IRequiredActor<OrganizationActor> actor,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new
            {
                code = "invalid_name",
                message = "Name is required.",
            });
        }
        if (!OrgSlug.TryNormalize(request.Slug, out var slug, out var slugError))
        {
            return Results.BadRequest(new { code = "invalid_org_slug", message = slugError });
        }

        var cmd = new CreateOrganizationCommand(request.Name, slug, request.PrimaryDomain);
        var org = await actor.ActorRef.Ask<Organization>(cmd, TimeSpan.FromSeconds(5), ct);
        return Results.Created($"/v1/orgs/{org.Id}", org);
    }
}
