using Akka.Actor;
using Akka.Hosting;
using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Authentication.Identities;

public static class CreateIdentity
{
    public sealed record Request(string Username, string Email, string? DisplayName);

    public static async Task<IResult> Handle(
        Guid orgId,
        Request request,
        HttpContext http,
        IRequiredActor<IdentityActor> actor,
        CancellationToken ct)
    {
        var session = http.GetSession();
        if (session.OrgId != orgId)
            return Results.Forbid();

        if (!Username.TryNormalize(request.Username, out var username, out var usernameError))
            return Results.BadRequest(new { code = "invalid_username", message = usernameError });

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return Results.BadRequest(new { code = "invalid_email", message = "Valid email required." });

        var cmd = new CreateIdentityCommand(orgId, username, request.Email, request.DisplayName);
        var result = await actor.ActorRef.Ask<IdentityResult>(cmd, TimeSpan.FromSeconds(5), ct);

        return result switch
        {
            { ErrorCode: "email_taken" }    => Results.Conflict(new { code = "email_taken", message = "Email is already in use." }),
            { ErrorCode: "username_taken" } => Results.Conflict(new { code = "username_taken", message = "That username is taken." }),
            { Identity: { } identity }      => Results.Created($"/v1/identities/{identity.Id}", identity),
            _                               => Results.Problem("Failed to create identity."),
        };
    }
}
