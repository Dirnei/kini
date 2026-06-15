using Akka.Hosting;
using Fido2NetLib;
using Kini.Api.ApiTokens;
using Kini.Api.Audit;
using Kini.Api.Authentication.Challenges;
using Kini.Api.Authentication.Credentials;
using Kini.Api.Authentication.Identities;
using Kini.Api.Authentication.Registration;
using Kini.Api.Authentication.Sessions;
using Kini.Api.Authentication.SignIn;
using Kini.Api.Keys;
using Kini.Api.Mongo;
using Kini.Api.Organizations;
using Kini.Api.WellKnown;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ------------------------------------------------------
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));

// --- MongoDB ------------------------------------------------------------
MongoSetup.RegisterConventions();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    return new MongoClient(opts.ConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var opts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    return client.GetDatabase(opts.Database);
});

// --- Slice collections (each slice owns its Mongo wrapper) --------------
builder.Services.AddSingleton<OrganizationsCollection>();
builder.Services.AddSingleton<IdentitiesCollection>();
builder.Services.AddSingleton<SessionsCollection>();
builder.Services.AddSingleton<ChallengesCollection>();
builder.Services.AddSingleton<SshCredentialsCollection>();
builder.Services.AddSingleton<WebAuthnCredentialsCollection>();
builder.Services.AddSingleton<KeysCollection>();
builder.Services.AddSingleton<ApiTokensCollection>();
builder.Services.AddSingleton<AuditCollection>();
builder.Services.AddSingleton<AuditLog>();

// --- Authentication helpers --------------------------------------------
builder.Services.AddSingleton<IssueSession>();
builder.Services.AddSingleton<ChallengeStore>();
builder.Services.AddSingleton<SshSignatureVerifier>();

// --- WebAuthn (Fido2NetLib) --------------------------------------------
// For local dev, RP ID is "localhost" (browsers accept it without TLS).
// For production we'll bind to the deployed origin and migrate via config.
builder.Services.AddFido2(options =>
{
    options.ServerDomain = builder.Configuration["WebAuthn:RpId"] ?? "localhost";
    options.ServerName = builder.Configuration["WebAuthn:RpName"] ?? "Kini";
    options.Origins = new HashSet<string>(
        builder.Configuration.GetSection("WebAuthn:Origins").Get<string[]>()
            ?? new[] { "http://localhost:5001", "http://localhost:5173" });
    options.TimestampDriftTolerance = 300_000;
});

// --- Akka.NET (one actor system; per-slice actors registered inline) ----
builder.Services.AddAkka("Kini", (akka, _) =>
{
    akka.WithActors((system, registry, resolver) =>
    {
        var orgs = system.ActorOf(resolver.Props<OrganizationActor>(), "organizations");
        registry.Register<OrganizationActor>(orgs);

        var ids = system.ActorOf(resolver.Props<IdentityActor>(), "identities");
        registry.Register<IdentityActor>(ids);
    });
});

var app = builder.Build();

// --- One-time index setup -----------------------------------------------
using (var scope = app.Services.CreateScope())
using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
{
    // Backfill missing fields BEFORE EnsureIndexes — the new unique index on
    // username would otherwise fail on legacy docs. Role backfill is also here
    // for the same reason (record deserialization needs Role to be present).
    var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await IdentitiesBackfill.BackfillUsernames(db, cts.Token);
    await IdentitiesBackfill.BackfillRoles(db, cts.Token);

    await scope.ServiceProvider.GetRequiredService<OrganizationsCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<IdentitiesCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<SessionsCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<ChallengesCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<SshCredentialsCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<WebAuthnCredentialsCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<KeysCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<ApiTokensCollection>().EnsureIndexes(cts.Token);
    await scope.ServiceProvider.GetRequiredService<AuditCollection>().EnsureIndexes(cts.Token);
}

// --- HTTP pipeline ------------------------------------------------------
app.UseStaticFiles();

// Resolve bearer tokens to Sessions on every request. Endpoints opt in to
// requiring the session via .RequireSession().
app.UseMiddleware<BearerAuthMiddleware>();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// --- Slice endpoint registration ----------------------------------------
app.MapOrganizationsEndpoints();
app.MapIdentitiesEndpoints();
app.MapCredentialsEndpoints();
app.MapSignInEndpoints();
app.MapSessionsEndpoints();
app.MapRegistrationEndpoints();
app.MapKeysEndpoints();
app.MapWellKnownEndpoints();
app.MapApiTokensEndpoints();
app.MapAuditEndpoints();

// SPA fallback (catches anything not matched above; serves index.html for client-side routing)
app.MapFallbackToFile("index.html");

app.Run();

// Expose the auto-generated top-level-statements Program class so the
// integration test project can use WebApplicationFactory<Program>.
public partial class Program { }
