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
using System.IO;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for LazyBsonDocuments.
    /// </summary>
    public class LazyBsonDocumentSerializer : BsonBaseSerializer<LazyBsonDocument>
    {
        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override LazyBsonDocument Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.Document:
                    var slice = bsonReader.ReadRawBsonDocument();
                    return new LazyBsonDocument(slice);

                default:
                    var message = string.Format("Cannot deserialize LazyBsonDocument from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, LazyBsonDocument value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var slice = value.Slice;
                if (slice == null)
                {
                    context.SerializeWithChildContext(BsonDocumentSerializer.Instance, value);
                }
                else
                {
                    using (var clonedSlice = slice.GetSlice(0, slice.Length))
                    {
                        bsonWriter.WriteRawBsonDocument(clonedSlice);
                    }
                }
            }
        }
    }
}
