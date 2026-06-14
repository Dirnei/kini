using Kini.Api.Audit;
using Kini.Api.Authentication.Sessions;
using MongoDB.Driver;

namespace Kini.Api.ApiTokens;

public static class ApiTokensEndpoints
{
    public static IEndpointRouteBuilder MapApiTokensEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/api-tokens").WithTags("tokens");
        grp.MapGet("/", List).RequireSession();
        grp.MapPost("/", Create).RequireSession();
        grp.MapDelete("/{tokenId:guid}", Revoke).RequireSession();
        return app;
    }

    public sealed record CreateRequest(string Name, string[]? Scopes);
    public sealed record CreateResponse(
        Guid Id, Guid IdentityId, Guid OrgId, string Name, string[] Scopes,
        DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, DateTimeOffset? RevokedAt,
        string Token);

    private static async Task<IResult> List(HttpContext http, ApiTokensCollection tokens, CancellationToken ct)
    {
        var session = http.GetSession();
        var docs = await tokens.Collection
            .Find(t => t.IdentityId == session.IdentityId && t.RevokedAt == null)
            .ToListAsync(ct);
        return Results.Ok(docs);
    }

    private static async Task<IResult> Create(
        CreateRequest request,
        HttpContext http,
        ApiTokensCollection tokens,
        AuditLog audit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { code = "invalid_name" });

        var session = http.GetSession();
        var (plaintext, hash) = TokenGenerator.Issue();
        var token = new ApiToken(
            Id: Guid.NewGuid(),
            IdentityId: session.IdentityId,
            OrgId: session.OrgId,
            Name: request.Name.Trim(),
            TokenHash: hash,
            Scopes: request.Scopes ?? Array.Empty<string>(),
            CreatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null,
            RevokedAt: null);

        await tokens.Collection.InsertOneAsync(token, cancellationToken: ct);
        await audit.Record(http, AuditAction.ApiTokenCreated,
            new AuditTarget("api-token", token.Id, token.Name), ct: ct);

        return Results.Created($"/v1/api-tokens/{token.Id}", new CreateResponse(
            token.Id, token.IdentityId, token.OrgId, token.Name, token.Scopes,
            token.CreatedAt, token.LastUsedAt, token.RevokedAt, plaintext));
    }

    private static async Task<IResult> Revoke(
        Guid tokenId,
        HttpContext http,
        ApiTokensCollection tokens,
        AuditLog audit,
        CancellationToken ct)
    {
        var session = http.GetSession();
        var result = await tokens.Collection.FindOneAndUpdateAsync(
            t => t.Id == tokenId && t.IdentityId == session.IdentityId && t.RevokedAt == null,
            Builders<ApiToken>.Update.Set(t => t.RevokedAt, DateTimeOffset.UtcNow),
            new FindOneAndUpdateOptions<ApiToken> { ReturnDocument = ReturnDocument.After },
            ct);
        if (result is null) return Results.NotFound();

        await audit.Record(http, AuditAction.ApiTokenRevoked,
            new AuditTarget("api-token", result.Id, result.Name), ct: ct);

        return Results.NoContent();
    }
}
