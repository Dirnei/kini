using Kini.Api.Authentication.Sessions;

namespace Kini.Api.Keys;

public static class KeysEndpoints
{
    public static IEndpointRouteBuilder MapKeysEndpoints(this IEndpointRouteBuilder app)
    {
        var idGrp = app.MapGroup("/v1/identities/{email}/keys").WithTags("keys");
        idGrp.MapGet("/", ListKeysForIdentity.Handle).RequireSession();
        idGrp.MapPost("/", UploadKey.Handle).RequireSession();

        var keyGrp = app.MapGroup("/v1/keys/{keyId:guid}").WithTags("keys");
        keyGrp.MapGet("/", GetKey.Handle).RequireSession();
        keyGrp.MapDelete("/", RevokeKey.Delete).RequireSession();
        keyGrp.MapPost("/revoke", RevokeKey.Handle).RequireSession();

        return app;
    }
}
