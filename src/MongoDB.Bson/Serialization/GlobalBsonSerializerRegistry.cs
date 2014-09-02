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
    /// Implementation of a bson serializer registry that uses the global BsonSerializer methods.
    /// </summary>
    public class GlobalBsonSerializerRegistry : IBsonSerializerRegistry
    {
        // private static fields
        private static readonly GlobalBsonSerializerRegistry __instance = new GlobalBsonSerializerRegistry();

        // public static properties
        /// <summary>
        /// Gets the instance of the global registry.
        /// </summary>
        public static GlobalBsonSerializerRegistry Instance
        {
            get { return __instance; }
        }

        // constructors
        private GlobalBsonSerializerRegistry()
        {
        }

        // public methods
        /// <summary>
        /// Gets the serializer for the specified <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The serializer.
        /// </returns>
        public IBsonSerializer GetSerializer(Type type)
        {
            return BsonSerializer.LookupSerializer(type);
        }

        /// <summary>
        /// Gets the serializer for the specified <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// The serializer.
        /// </returns>
        public IBsonSerializer<T> GetSerializer<T>()
        {
            return BsonSerializer.LookupSerializer<T>();
        }
    }
}
