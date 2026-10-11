using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// BSON conventions for the SlotBook types (AD-9): GUIDs as standard UUIDs, enums as strings,
/// camelCase element names. Unknown elements are ignored on read, which is how an old binary reads
/// a document the new one wrote (expand / contract). Applied once per process.
/// </summary>
internal static class MongoConventions
{
    private static readonly Lock _gate = new();
    private static bool _applied;

    public static void Apply()
    {
        lock (_gate)
        {
            if (_applied)
            {
                return;
            }

            try
            {
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            }
            catch (BsonSerializationException)
            {
                // Already registered; the driver default is the standard representation.
            }

            ConventionRegistry.Register(
                "SlotBook",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new EnumRepresentationConvention(BsonType.String),
                    new IgnoreExtraElementsConvention(ignoreExtraElements: true),
                },
                static type => type.Assembly.GetName().Name?.StartsWith("SlotBook.", StringComparison.Ordinal) == true);
            _applied = true;
        }
    }
}
