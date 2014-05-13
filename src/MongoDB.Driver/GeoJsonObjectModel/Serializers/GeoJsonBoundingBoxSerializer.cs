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
    public class GeoJsonBoundingBoxSerializer<TCoordinates> : ClassSerializerBase<GeoJsonBoundingBox<TCoordinates>> where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private readonly IBsonSerializer<TCoordinates> _coordinatesSerializer = BsonSerializer.LookupSerializer<TCoordinates>();

        // protected methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        protected override GeoJsonBoundingBox<TCoordinates> DeserializeValue(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var flattenedArray = BsonArraySerializer.Instance.Deserialize(context.CreateChild(typeof(BsonArray)));
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

            using (var documentReader = new BsonDocumentReader(document))
            {
                var documentContext = BsonDeserializationContext.CreateRoot(documentReader, typeof(BsonDocument));
                documentReader.ReadStartDocument();
                documentReader.ReadName("min");
                var min = documentContext.DeserializeWithChildContext(_coordinatesSerializer);
                documentReader.ReadName("max");
                var max = documentContext.DeserializeWithChildContext(_coordinatesSerializer);
                documentReader.ReadEndDocument();

                return new GeoJsonBoundingBox<TCoordinates>(min, max);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, GeoJsonBoundingBox<TCoordinates> value)
        {
            var bsonWriter = context.Writer;

            // serialize min and max to a dummy document and then flatten the two arrays and serialize that
            var document = new BsonDocument();
            using (var documentWriter = new BsonDocumentWriter(document))
            {
                var documentContext = BsonSerializationContext.CreateRoot(documentWriter, typeof(BsonDocument));
                documentWriter.WriteStartDocument();
                documentWriter.WriteName("min");
                documentContext.SerializeWithChildContext(_coordinatesSerializer, value.Min);
                documentWriter.WriteName("max");
                documentContext.SerializeWithChildContext(_coordinatesSerializer, value.Max);
                documentWriter.WriteEndDocument();
            }

            var flattenedArray = new BsonArray();
            flattenedArray.AddRange(document["min"].AsBsonArray);
            flattenedArray.AddRange(document["max"].AsBsonArray);

            context.SerializeWithChildContext(BsonArraySerializer.Instance, flattenedArray);
        }
    }
}
