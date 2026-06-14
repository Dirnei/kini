using Akka.Actor;
using Akka.Hosting;

namespace Kini.Api.Organizations;

public static class CreateOrganization
{
    public sealed record Request(string Name, string? PrimaryDomain);

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

        var cmd = new CreateOrganizationCommand(request.Name, request.PrimaryDomain);
        var org = await actor.ActorRef.Ask<Organization>(cmd, TimeSpan.FromSeconds(5), ct);
        return Results.Created($"/v1/orgs/{org.Id}", org);
    }
}
