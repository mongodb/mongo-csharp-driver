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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel.Serializers
{
    /// <summary>
    /// Represents a serializer for a GeoJsonPolygonCoordinates value.
    /// </summary>
    public class GeoJsonPolygonCoordinatesSerializer<TCoordinates> : BsonBaseSerializer<GeoJsonPolygonCoordinates<TCoordinates>> where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private readonly IBsonSerializer<GeoJsonLinearRingCoordinates<TCoordinates>> _linearRingCoordinatesSerializer = BsonSerializer.LookupSerializer<GeoJsonLinearRingCoordinates<TCoordinates>>();

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        public override GeoJsonPolygonCoordinates<TCoordinates> Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                var holes = new List<GeoJsonLinearRingCoordinates<TCoordinates>>();

                bsonReader.ReadStartArray();
                var exterior = context.DeserializeWithChildContext(_linearRingCoordinatesSerializer);
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var hole = context.DeserializeWithChildContext(_linearRingCoordinatesSerializer);
                    holes.Add(hole);
                }
                bsonReader.ReadEndArray();

                return new GeoJsonPolygonCoordinates<TCoordinates>(exterior, holes);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, GeoJsonPolygonCoordinates<TCoordinates> value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                bsonWriter.WriteStartArray();
                context.SerializeWithChildContext(_linearRingCoordinatesSerializer, value.Exterior);
                foreach (var hole in value.Holes)
                {
                    context.SerializeWithChildContext(_linearRingCoordinatesSerializer, hole);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }
}
