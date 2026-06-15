using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Kini.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real <see cref="Program"/> against the testcontainer Mongo,
/// with a fresh database name per factory instance so concurrent tests
/// (or repeated runs in the same fixture) don't share state.
/// </summary>
public sealed class KiniWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    public string DatabaseName { get; } = $"kini-test-{Guid.NewGuid():N}";

    public KiniWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = _connectionString,
                ["Mongo:Database"]         = DatabaseName,
                // Tests don't touch WebAuthn but the host registers Fido2 at startup,
                // so we give it a value it'll accept.
                ["WebAuthn:RpId"]          = "localhost",
                ["WebAuthn:RpName"]        = "Kini Tests",
                ["WebAuthn:Origins:0"]     = "http://localhost",
            });
        });
    }
}
