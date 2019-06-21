using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that sets representation of a string id class member to ObjectId in BSON with a StringObjectIdGenerator.
    /// </summary>
    public class StringIdStoredAsObjectIdConvention : ConventionBase, IMemberMapConvention
    {
        /// <summary>
        /// Applies a post processing modification to the class map.
        /// </summary>
        /// <param name="memberMap">The BsonMemberMap map.</param>
        /// <remarks>This method sets both the serializer and the IdGenerator on the id member field.</remarks>
        public void Apply(BsonMemberMap memberMap)
        {
            var idMemberMap = memberMap.ClassMap?.IdMemberMap;

            if (idMemberMap == null)
            {
                return;
            }

            if (idMemberMap.MemberType != typeof(string))
            {
                return;
            }

            if (idMemberMap.IdGenerator != null)
            {
                return;
            }

            var idSerializer = idMemberMap.GetSerializer();

            if (idSerializer is StringSerializer stringSerializer && stringSerializer.Representation == BsonType.String)
            {
                idMemberMap.SetSerializer(new StringSerializer(representation: BsonType.ObjectId));
                idMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
            }
        }
    }
}
