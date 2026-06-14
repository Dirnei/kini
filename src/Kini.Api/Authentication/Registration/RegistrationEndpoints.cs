namespace Kini.Api.Authentication.Registration;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/sign-up", SignUp.Handle).WithTags("registration");
        return app;
    }
}
