using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Base provider for serialization providers.
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
        /// Creates the serializer from a serializer type definition and type arguments.
        /// </summary>
        /// <param name="serializerTypeDefinition">The serializer type definition.</param>
        /// <param name="genericArguments">The generic arguments.</param>
        /// <returns></returns>
        protected virtual IBsonSerializer CreateGenericSerializer(Type serializerTypeDefinition, params Type[] genericArguments)
        {
            var type = serializerTypeDefinition.MakeGenericType(genericArguments);
            return CreateSerializer(type);
        }

        /// <summary>
        /// Creates the serializer.
        /// </summary>
        /// <param name="serializerType">The serializer type.</param>
        /// <returns></returns>
        protected virtual IBsonSerializer CreateSerializer(Type serializerType)
        {
            return (IBsonSerializer)Activator.CreateInstance(serializerType);
        }
    }
}
