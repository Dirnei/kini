using Testcontainers.MongoDb;
using Xunit;

namespace Kini.Api.Tests.Infrastructure;

/// <summary>
/// Shared per-collection fixture that boots an ephemeral mongo:8 container
/// once and tears it down at the end. Each test class that needs a DB
/// declares <c>IClassFixture&lt;KiniMongoFixture&gt;</c>; each test inside
/// then uses a fresh database name so tests can't collide.
/// </summary>
public sealed class KiniMongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
