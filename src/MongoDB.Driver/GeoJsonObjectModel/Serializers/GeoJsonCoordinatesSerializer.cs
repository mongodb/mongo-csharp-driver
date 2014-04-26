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
    /// Represents a serializer for a GeoJsonCoordinates value.
    /// </summary>
    public class GeoJsonCoordinatesSerializer : BsonBaseSerializer<GeoJsonCoordinates>
    {
        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        /// <exception cref="System.InvalidOperationException">Only concrete subclasses of GeoJsonCoordinates can be serialized.</exception>
        public override GeoJsonCoordinates Deserialize(BsonDeserializationContext context)
        {
            throw new InvalidOperationException("Only concrete subclasses of GeoJsonCoordinates can be serialized.");
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.InvalidOperationException">Only concrete subclasses of GeoJsonCoordinates can be serialized.</exception>
        public override void Serialize(BsonSerializationContext context, GeoJsonCoordinates value)
        {
            throw new InvalidOperationException("Only concrete subclasses of GeoJsonCoordinates can be serialized.");
        }
    }
}
