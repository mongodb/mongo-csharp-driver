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

using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel.Serializers
{
    /// <summary>
    /// Represents a serializer for a GeoJsonPolygon value.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public class GeoJsonPolygonSerializer<TCoordinates> : ClassSerializerBase<GeoJsonPolygon<TCoordinates>> where TCoordinates : GeoJsonCoordinates
    {
        // private constants
        private static class Flags
        {
            public const long Coordinates = 16;
        }

        // private fields
        private readonly IBsonSerializer<GeoJsonPolygonCoordinates<TCoordinates>> _coordinatesSerializer = BsonSerializer.LookupSerializer<GeoJsonPolygonCoordinates<TCoordinates>>();
        private readonly GeoJsonObjectSerializerHelper<TCoordinates> _helper;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoJsonPointSerializer{TCoordinates}"/> class.
        /// </summary>
        public GeoJsonPolygonSerializer()
        {
            _helper = new GeoJsonObjectSerializerHelper<TCoordinates>
            (
                "Polygon",
                new SerializerHelper.Member("coordinates", Flags.Coordinates)
            );
        }

        // protected methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        protected override GeoJsonPolygon<TCoordinates> DeserializeValue(BsonDeserializationContext context)
        {
            var args = new GeoJsonObjectArgs<TCoordinates>();
            GeoJsonPolygonCoordinates<TCoordinates> coordinates = null;

            _helper.DeserializeMembers(context, (elementName, flag) =>
            {
                switch (flag)
                {
                    case Flags.Coordinates: coordinates = DeserializeCoordinates(context); break;
                    default: _helper.DeserializeBaseMember(context, elementName, flag, args); break;
                }
            });

            return new GeoJsonPolygon<TCoordinates>(args, coordinates);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, GeoJsonPolygon<TCoordinates> value)
        {
            _helper.SerializeMembers(context, value, SerializeDerivedMembers);
        }

        // private methods
        private GeoJsonPolygonCoordinates<TCoordinates> DeserializeCoordinates(BsonDeserializationContext context)
        {
            return context.DeserializeWithChildContext(_coordinatesSerializer);
        }

        private void SerializeCoordinates(BsonSerializationContext context, GeoJsonPolygonCoordinates<TCoordinates> coordinates)
        {
            context.Writer.WriteName("coordinates");
            context.SerializeWithChildContext(_coordinatesSerializer, coordinates);
        }

        private void SerializeDerivedMembers(BsonSerializationContext context, GeoJsonPolygon<TCoordinates> value)
        {
            SerializeCoordinates(context, value.Coordinates);
        }
    }
}
