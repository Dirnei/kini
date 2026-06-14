namespace Kini.Api.Mongo;

public sealed class MongoOptions
{
    public required string ConnectionString { get; init; }
    public required string Database { get; init; }
}
