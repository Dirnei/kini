using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Kini.Api.Mongo;

public static class MongoSetup
{
    private static int _registered;

    public static void RegisterConventions()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1) return;

        // MongoDB.Driver 3.x defaults GuidRepresentation to Unspecified and
        // refuses to serialize Guid fields until one is chosen. Standard =
        // UUID binary subtype 4 (RFC 4122), the modern recommendation.
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String),
        };

        ConventionRegistry.Register("kini", pack, _ => true);
    }
}
