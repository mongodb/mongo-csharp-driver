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

using System;
using System.Collections.Generic;
using System.Dynamic;
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
            get => BsonSerializationDomain.Default.BsonDefaults.DynamicArraySerializer;
            set => BsonSerializationDomain.Default.BsonDefaults.DynamicArraySerializer = value;
        }

        /// <summary>
        /// Gets or sets the dynamic document serializer.
        /// </summary>
        public static IBsonSerializer DynamicDocumentSerializer
        {
            get => BsonSerializationDomain.Default.BsonDefaults.DynamicDocumentSerializer;
            set => BsonSerializationDomain.Default.BsonDefaults.DynamicDocumentSerializer = value;
        }

        /* DOMAIN-API We should modify the API to have those two values (and in the writer/reader settings where they are used) be nullable.
         * The problem is that we need to now when these values have been set externally or not. If they have not, then they should
         * be retrieved from the closest domain.
         */

        /// <summary>
        /// Gets or sets the default max document size. The default is 4MiB.
        /// </summary>
        public static int MaxDocumentSize
        {
            get => BsonSerializationDomain.Default.BsonDefaults.MaxDocumentSize;
            set => BsonSerializationDomain.Default.BsonDefaults.MaxDocumentSize = value;
        }

        /// <summary>
        /// Gets or sets the default max serialization depth (used to detect circular references during serialization). The default is 100.
        /// </summary>
        public static int MaxSerializationDepth
        {
            get => BsonSerializationDomain.Default.BsonDefaults.MaxSerializationDepth;
            set => BsonSerializationDomain.Default.BsonDefaults.MaxSerializationDepth = value;
        }
    }
}
