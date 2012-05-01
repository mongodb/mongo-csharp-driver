using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents the class map serialization provider.
    /// </summary>
    internal class BsonClassMapSerializationProvider : IBsonSerializationProvider
    {
        /// <summary>
        /// Gets the serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The serializer.</returns>
        public IBsonSerializer GetSerializer(Type type)
        {
            if ((type.IsClass || (type.IsValueType && !type.IsPrimitive)) &&
                !typeof(Array).IsAssignableFrom(type) &&
                !typeof(Enum).IsAssignableFrom(type))
            {
                var classMap = BsonClassMap.LookupClassMap(type);
                return new BsonClassMapSerializer(classMap);
            }

            return null;
        }
    }
}