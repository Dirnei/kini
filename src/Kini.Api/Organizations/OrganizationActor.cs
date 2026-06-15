using Akka.Actor;
using Akka.Event;
using MongoDB.Driver;

namespace Kini.Api.Organizations;

public sealed record CreateOrganizationCommand(string Name, string Slug, string? PrimaryDomain);

public sealed class OrganizationActor : ReceiveActor
{
    private readonly OrganizationsCollection _orgs;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public OrganizationActor(OrganizationsCollection orgs)
    {
        _orgs = orgs;
        ReceiveAsync<CreateOrganizationCommand>(HandleCreate);
    }

    private async Task HandleCreate(CreateOrganizationCommand cmd)
    {
        var org = new Organization(
            Id: Guid.NewGuid(),
            Name: cmd.Name,
            Slug: cmd.Slug,
            PrimaryDomain: string.IsNullOrWhiteSpace(cmd.PrimaryDomain) ? null : cmd.PrimaryDomain.Trim().ToLowerInvariant(),
            CreatedAt: DateTimeOffset.UtcNow);

        await _orgs.Collection.InsertOneAsync(org);
        _log.Info("Organization created: {Id} {Name}", org.Id, org.Name);
        Sender.Tell(org);
    }
}
