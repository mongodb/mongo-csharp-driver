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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel.Serializers
{
    /// <summary>
    /// Represents a serializer for a GeoJsonNamedCoordinateReferenceSystem value.
    /// </summary>
    public class GeoJsonNamedCoordinateReferenceSystemSerializer : BsonBaseSerializer<GeoJsonNamedCoordinateReferenceSystem>
    {
        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        public override GeoJsonNamedCoordinateReferenceSystem Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                bsonReader.ReadStartDocument();
                var type = bsonReader.ReadString("type");
                if (type != "name")
                {
                    var message = string.Format("Expected type to be 'name'.");
                    throw new FormatException(message);
                }
                bsonReader.ReadName("properties");
                bsonReader.ReadStartDocument();
                var name = bsonReader.ReadString("name");
                bsonReader.ReadEndDocument();
                bsonReader.ReadEndDocument();

                return new GeoJsonNamedCoordinateReferenceSystem(name);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, GeoJsonNamedCoordinateReferenceSystem value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("type", "name");
                bsonWriter.WriteStartDocument("properties");
                bsonWriter.WriteString("name", value.Name);
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }
        }
    }
}
