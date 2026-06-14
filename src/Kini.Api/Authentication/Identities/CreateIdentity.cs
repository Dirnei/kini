using Akka.Actor;
using Akka.Hosting;
using Kini.Api.Audit;
using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Authentication.Identities;

public static class CreateIdentity
{
    public sealed record Request(string Username, string Email, string? DisplayName);

    public static async Task<IResult> Handle(
        Guid orgId,
        Request request,
        HttpContext http,
        IdentitiesCollection identities,
        IRequiredActor<IdentityActor> actor,
        AuditLog audit,
        CancellationToken ct)
    {
        var session = http.GetSession();
        if (session.OrgId != orgId)
            return Results.Forbid();

        // Only Owners can add members to an org. Members can only manage themselves.
        var caller = await identities.FindById(session.IdentityId, ct);
        if (caller is null || caller.Role != IdentityRole.Owner)
            return Results.Forbid();

        if (!Username.TryNormalize(request.Username, out var username, out var usernameError))
            return Results.BadRequest(new { code = "invalid_username", message = usernameError });

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return Results.BadRequest(new { code = "invalid_email", message = "Valid email required." });

        var cmd = new CreateIdentityCommand(orgId, username, request.Email, IdentityRole.Member, request.DisplayName);
        var result = await actor.ActorRef.Ask<IdentityResult>(cmd, TimeSpan.FromSeconds(5), ct);

        if (result.Identity is { } created)
        {
            await audit.Record(http, AuditAction.IdentityCreated,
                new AuditTarget("identity", created.Id, created.Username), ct: ct);
            return Results.Created($"/v1/identities/{created.Id}", created);
        }

        return result.ErrorCode switch
        {
            "email_taken"    => Results.Conflict(new { code = "email_taken", message = "Email is already in use." }),
            "username_taken" => Results.Conflict(new { code = "username_taken", message = "That username is taken." }),
            _                => Results.Problem("Failed to create identity."),
        };
    }
}
