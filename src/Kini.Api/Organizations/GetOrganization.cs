using MongoDB.Driver;

namespace Kini.Api.Organizations;

public static class GetOrganization
{
    public static async Task<IResult> Handle(
        Guid orgId,
        OrganizationsCollection orgs,
        CancellationToken ct)
    {
        var org = await orgs.Collection
            .Find(o => o.Id == orgId)
            .FirstOrDefaultAsync(ct);

        return org is null ? Results.NotFound() : Results.Ok(org);
    }
}
