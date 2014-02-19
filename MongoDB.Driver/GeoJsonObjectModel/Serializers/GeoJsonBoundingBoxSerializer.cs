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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel.Serializers
{
    /// <summary>
    /// Represents a serializer for a GeoJsonBoundingBox value.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public class GeoJsonBoundingBoxSerializer<TCoordinates> : BsonBaseSerializer where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private readonly IBsonSerializer _coordinatesSerializer = BsonSerializer.LookupSerializer(typeof(TCoordinates));

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>
        /// An object.
        /// </returns>
        /// <exception cref="System.FormatException">Bounding box array does not have an even number of values.</exception>
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                var flattenedArray = (BsonArray)BsonArraySerializer.Instance.Deserialize(bsonReader, typeof(BsonArray), null);
                if ((flattenedArray.Count % 2) != 0)
                {
                    throw new FormatException("Bounding box array does not have an even number of values.");
                }
                var half = flattenedArray.Count / 2;

                // create a dummy document with a min and a max and then deserialize the min and max coordinates from there
                var document = new BsonDocument
                {
                    { "min", new BsonArray(flattenedArray.Take(half)) },
                    { "max", new BsonArray(flattenedArray.Skip(half)) }
                };

                using (var documentReader = BsonReader.Create(document))
                {
                    documentReader.ReadStartDocument();
                    documentReader.ReadName("min");
                    var min = (TCoordinates)_coordinatesSerializer.Deserialize(documentReader, typeof(TCoordinates), null);
                    documentReader.ReadName("max");
                    var max = (TCoordinates)_coordinatesSerializer.Deserialize(documentReader, typeof(TCoordinates), null);
                    documentReader.ReadEndDocument();

                    return new GeoJsonBoundingBox<TCoordinates>(min, max);
                }
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var boundingBox = (GeoJsonBoundingBox<TCoordinates>)value;

                // serialize min and max to a dummy document and then flatten the two arrays and serialize that
                var document = new BsonDocument();
                using (var documentWriter = BsonWriter.Create(document))
                {
                    documentWriter.WriteStartDocument();
                    documentWriter.WriteName("min");
                    _coordinatesSerializer.Serialize(documentWriter, typeof(TCoordinates), boundingBox.Min, null);
                    documentWriter.WriteName("max");
                    _coordinatesSerializer.Serialize(documentWriter, typeof(TCoordinates), boundingBox.Max, null);
                    documentWriter.WriteEndDocument();
                }

                var flattenedArray = new BsonArray();
                flattenedArray.AddRange(document["min"].AsBsonArray);
                flattenedArray.AddRange(document["max"].AsBsonArray);

                BsonArraySerializer.Instance.Serialize(bsonWriter, typeof(BsonArray), flattenedArray, null);
            }
        }
    }
}
