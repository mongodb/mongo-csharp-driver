/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Base class for serialization providers.
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
        /// <param name="typeArguments">The type arguments.</param>
        /// <returns></returns>
        protected virtual IBsonSerializer CreateGenericSerializer(Type serializerTypeDefinition, params Type[] typeArguments)
        {
            var serializerType = serializerTypeDefinition.MakeGenericType(typeArguments);
            return CreateSerializer(serializerType);
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
