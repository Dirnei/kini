using System.Net;
using System.Net.Http.Json;
using Kini.Api.Tests.Infrastructure;
using Xunit;

namespace Kini.Api.Tests;

/// <summary>
/// End-to-end happy path: anonymous sign-up creates the org + identity +
/// credential + published key + session in one atomic call, and the public
/// `/{username}.keys` endpoint (no auth) immediately resolves the key.
///
/// This is the same smoke flow we've been verifying by hand via curl,
/// promoted to a test so regressions get caught without re-running
/// the live stack.
/// </summary>
public class SignUpAndResolveTests : IClassFixture<KiniMongoFixture>
{
    // A real, well-formed ssh-ed25519 line. The base64 blob is a valid
    // SSH wire-format ed25519 pubkey; the parser accepts it and produces
    // a deterministic fingerprint. Public-by-nature, no secret here.
    private const string TestPubKey =
        "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAINaj3SEO727elkS0c4TlwxYynl9QijNZ36uwQCIl0z71 test@example";

    private readonly KiniMongoFixture _mongo;
    public SignUpAndResolveTests(KiniMongoFixture mongo) => _mongo = mongo;

    [Fact]
    public async Task SignUp_publishes_key_resolvable_at_three_url_shapes()
    {
        await using var app = new KiniWebApplicationFactory(_mongo.ConnectionString);
        var client = app.CreateClient();

        var signUp = await client.PostAsJsonAsync("/v1/sign-up", new
        {
            organizationName = "Test Co",
            orgSlug          = "testco",
            primaryDomain    = "testco.example",
            username         = "alice",
            email            = "alice@testco.example",
            sshPublicKey     = TestPubKey,
        });

        Assert.Equal(HttpStatusCode.Created, signUp.StatusCode);

        // Flat /{username}.keys
        var flat = await client.GetAsync("/alice.keys");
        Assert.Equal(HttpStatusCode.OK, flat.StatusCode);
        var flatBody = await flat.Content.ReadAsStringAsync();
        Assert.Contains("ssh-ed25519", flatBody);

        // Org-scoped via slug
        var slug = await client.GetAsync("/testco/alice.keys");
        Assert.Equal(HttpStatusCode.OK, slug.StatusCode);
        Assert.Equal(flatBody, await slug.Content.ReadAsStringAsync());

        // Org-scoped via primaryDomain (slug forbids dots, so the router is unambiguous)
        var domain = await client.GetAsync("/testco.example/alice.keys");
        Assert.Equal(HttpStatusCode.OK, domain.StatusCode);
        Assert.Equal(flatBody, await domain.Content.ReadAsStringAsync());

        // Wrong org → 404, not the wrong-tenant's key
        var wrong = await client.GetAsync("/notmyorg/alice.keys");
        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
    }

    [Fact]
    public async Task SignUp_with_duplicate_orgSlug_returns_conflict()
    {
        await using var app = new KiniWebApplicationFactory(_mongo.ConnectionString);
        var client = app.CreateClient();

        async Task<HttpResponseMessage> SignUp(string username, string email) =>
            await client.PostAsJsonAsync("/v1/sign-up", new
            {
                organizationName = "Twin Co",
                orgSlug          = "twinco",
                username,
                email,
                sshPublicKey     = TestPubKey,
            });

        var first  = await SignUp("alice", "alice@twin.example");
        var second = await SignUp("bob",   "bob@twin.example");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Healthz_returns_ok_without_auth()
    {
        await using var app = new KiniWebApplicationFactory(_mongo.ConnectionString);
        var client = app.CreateClient();

        var health = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }
}
