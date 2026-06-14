namespace Kini.Api.Authentication.SignIn;

public static class SignInEndpoints
{
    public static IEndpointRouteBuilder MapSignInEndpoints(this IEndpointRouteBuilder app)
    {
        var ssh = app.MapGroup("/v1/auth/ssh").WithTags("auth");
        ssh.MapPost("/challenge", IssueSshChallenge.Handle);
        ssh.MapPost("/verify", VerifySshChallenge.Handle);

        var wa = app.MapGroup("/v1/auth/webauthn").WithTags("auth");
        wa.MapPost("/begin", BeginWebAuthnAssertion.Handle);
        wa.MapPost("/complete", CompleteWebAuthnAssertion.Handle);

        return app;
    }
}
