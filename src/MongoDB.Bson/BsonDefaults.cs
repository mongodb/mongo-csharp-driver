/* Copyright 2010-present MongoDB Inc.
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

using MongoDB.Bson.Serialization;
namespace MongoDB.Bson
{
    /// <summary>
    /// A static helper class containing BSON defaults.
    /// </summary>
    public static class BsonDefaults
    {
        // public static properties
        /// <summary>
        /// Gets or sets the dynamic array serializer.
        /// </summary>
        public static IBsonSerializer DynamicArraySerializer
        {
            get => BsonSerializer.DefaultSerializationDomain.BsonDefaults.DynamicArraySerializer;
            set => BsonSerializer.DefaultSerializationDomain.BsonDefaults.DynamicArraySerializer = value;
        }

        /// <summary>
        /// Gets or sets the dynamic document serializer.
        /// </summary>
        public static IBsonSerializer DynamicDocumentSerializer
        {
            get => BsonSerializer.DefaultSerializationDomain.BsonDefaults.DynamicDocumentSerializer;
            set => BsonSerializer.DefaultSerializationDomain.BsonDefaults.DynamicDocumentSerializer = value;
        }

        /* DOMAIN-API DynamicSerializer are used only in a handful of serializers, so they should be removed from here (and possibly from the public API altogether).
         * MaxDocumentSize should probably be removed from the public API too, as it should come from the server.
         * MaxSerializationDepeth is definitely usedful. Does it make sense to keep it global...?
         */

        /// <summary>
        /// Gets or sets the default max document size. The default is 4MiB.
        /// </summary>
        public static int MaxDocumentSize
        {
            get => BsonSerializer.DefaultSerializationDomain.BsonDefaults.MaxDocumentSize;
            set => BsonSerializer.DefaultSerializationDomain.BsonDefaults.MaxDocumentSize = value;
        }

        /// <summary>
        /// Gets or sets the default max serialization depth (used to detect circular references during serialization). The default is 100.
        /// </summary>
        public static int MaxSerializationDepth
        {
            get => BsonSerializer.DefaultSerializationDomain.BsonDefaults.MaxSerializationDepth;
            set => BsonSerializer.DefaultSerializationDomain.BsonDefaults.MaxSerializationDepth = value;
        }
    }
}
