using Akka.Hosting;
using Kini.Api.Mongo;
using Kini.Api.Organizations;
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

// --- Akka.NET (one actor system; per-slice actors registered inline) ----
builder.Services.AddAkka("Kini", (akka, _) =>
{
    akka.WithActors((system, registry, resolver) =>
    {
        var orgs = system.ActorOf(resolver.Props<OrganizationActor>(), "organizations");
        registry.Register<OrganizationActor>(orgs);
    });
});

var app = builder.Build();

// --- One-time index setup -----------------------------------------------
using (var scope = app.Services.CreateScope())
using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
{
    var orgs = scope.ServiceProvider.GetRequiredService<OrganizationsCollection>();
    await orgs.EnsureIndexes(cts.Token);
}

// --- HTTP pipeline ------------------------------------------------------
app.UseStaticFiles();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// Slice endpoint registration. Each slice owns its routes.
app.MapOrganizationsEndpoints();

// SPA fallback (catches anything not matched above; serves index.html for client-side routing)
app.MapFallbackToFile("index.html");

app.Run();
