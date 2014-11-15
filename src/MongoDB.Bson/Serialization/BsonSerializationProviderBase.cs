using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Base provider for serializers.
    /// </summary>
    public abstract class BsonSerializationProviderBase : IBsonSerializationProvider
    {
        /// <summary>
        /// Gets a serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// A serializer.
        /// </returns>
        public abstract IBsonSerializer GetSerializer(Type type);

        /// <summary>
        /// Creates the generic serializer.
        /// </summary>
        /// <param name="typeDefinition">The type definition.</param>
        /// <param name="genericArguments">The generic arguments.</param>
        /// <returns></returns>
        protected virtual IBsonSerializer CreateGenericSerializer(Type typeDefinition, params Type[] genericArguments)
        {
            var type = typeDefinition.MakeGenericType(genericArguments);
            return CreateSerializer(type);
        }

        /// <summary>
        /// Creates the serializer.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        protected virtual IBsonSerializer CreateSerializer(Type type)
        {
            return (IBsonSerializer)Activator.CreateInstance(type);
        }
    }
}
