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
    /// Represents a serializer for System.Drawing.Size.
    /// </summary>
    public class DrawingSizeSerializer : BsonBaseSerializer<System.Drawing.Size>
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the DrawingSizeSerializer class.
        /// </summary>
        public DrawingSizeSerializer()
        {
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override System.Drawing.Size Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var width = bsonReader.ReadInt32("Width");
                    var height = bsonReader.ReadInt32("Height");
                    bsonReader.ReadEndDocument();
                    return new System.Drawing.Size(width, height);

                default:
                    var message = string.Format("Cannot deserialize System.Drawing.Size from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object of type System.Drawing.Size  to a BsonWriter.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, System.Drawing.Size value)
        {
            var bsonWriter = context.Writer;

            bsonWriter.WriteStartDocument();
            bsonWriter.WriteInt32("Width", value.Width);
            bsonWriter.WriteInt32("Height", value.Height);
            bsonWriter.WriteEndDocument();
        }
    }
}
