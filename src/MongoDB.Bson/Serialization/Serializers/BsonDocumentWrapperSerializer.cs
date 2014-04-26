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
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BsonDocumentWrappers.
    /// </summary>
    public class BsonDocumentWrapperSerializer : BsonBaseSerializer<BsonDocumentWrapper>
    {
        // private static fields
        private static BsonDocumentWrapperSerializer __instance = new BsonDocumentWrapperSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentWrapperSerializer class.
        /// </summary>
        public BsonDocumentWrapperSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonDocumentWrapperSerializer class.
        /// </summary>
        public static BsonDocumentWrapperSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonDocumentWrapper value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.IsUpdateDocument)
            {
                var savedCheckElementNames = bsonWriter.CheckElementNames;
                var savedCheckUpdateDocument = bsonWriter.CheckUpdateDocument;
                try
                {
                    bsonWriter.CheckElementNames = false;
                    bsonWriter.CheckUpdateDocument = true;
                    context.SerializeWithChildContext(value.Serializer, value.Wrapped);
                }
                finally
                {
                    bsonWriter.CheckElementNames = savedCheckElementNames;
                    bsonWriter.CheckUpdateDocument = savedCheckUpdateDocument;
                }
            }
            else
            {
                context.SerializeWithChildContext(value.Serializer, value.Wrapped);
            }
        }
    }
}
