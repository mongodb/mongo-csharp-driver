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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel.Serializers
{
    /// <summary>
    /// Represents a serializer for a GeoJsonFeature value.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public class GeoJsonFeatureSerializer<TCoordinates> : ClassSerializerBase<GeoJsonFeature<TCoordinates>> where TCoordinates : GeoJsonCoordinates
    {
        // private constants
        private static class Flags
        {
            public const long Geometry = 16;
            public const long Id = 32;
            public const long Properties = 64;
        }

        // private fields
        private readonly IBsonSerializer<GeoJsonGeometry<TCoordinates>> _geometrySerializer = BsonSerializer.LookupSerializer<GeoJsonGeometry<TCoordinates>>();
        private readonly GeoJsonObjectSerializerHelper<TCoordinates> _helper;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoJsonFeatureSerializer{TCoordinates}"/> class.
        /// </summary>
        public GeoJsonFeatureSerializer()
        {
            _helper = new GeoJsonObjectSerializerHelper<TCoordinates>
            (
                "Feature",
                new SerializerHelper.Member("geometry", Flags.Geometry),
                new SerializerHelper.Member("id", Flags.Id, isOptional: true),
                new SerializerHelper.Member("properties", Flags.Properties, isOptional: true)
            );
        }

        // protected methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        protected override GeoJsonFeature<TCoordinates> DeserializeValue(BsonDeserializationContext context)
        {
            var args = new GeoJsonFeatureArgs<TCoordinates>();
            GeoJsonGeometry<TCoordinates> geometry = null;

            _helper.DeserializeMembers(context, (elementName, flag) =>
            {
                switch (flag)
                {
                    case Flags.Geometry: geometry = DeserializeGeometry(context); break;
                    case Flags.Id: args.Id = DeserializeId(context); break;
                    case Flags.Properties: args.Properties = DeserializeProperties(context); break;
                    default: _helper.DeserializeBaseMember(context, elementName, flag, args); break;
                }
            });

            return new GeoJsonFeature<TCoordinates>(args, geometry);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, GeoJsonFeature<TCoordinates> value)
        {
            _helper.SerializeMembers(context, value, SerializeDerivedMembers);
        }

        // private methods
        private GeoJsonGeometry<TCoordinates> DeserializeGeometry(BsonDeserializationContext context)
        {
            return context.DeserializeWithChildContext(_geometrySerializer);
        }

        private BsonValue DeserializeId(BsonDeserializationContext context)
        {
            return context.DeserializeWithChildContext(BsonValueSerializer.Instance);
        }

        private BsonDocument DeserializeProperties(BsonDeserializationContext context)
        {
            return context.DeserializeWithChildContext(BsonDocumentSerializer.Instance);
        }

        private void SerializeDerivedMembers(BsonSerializationContext context, GeoJsonFeature<TCoordinates> value)
        {
            SerializeGeometry(context, value.Geometry);
            SerializeId(context, value.Id);
            SerializeProperties(context, value.Properties);
        }

        private void SerializeGeometry(BsonSerializationContext context, GeoJsonGeometry<TCoordinates> geometry)
        {
            context.Writer.WriteName("geometry");
            context.SerializeWithChildContext(_geometrySerializer, geometry);
        }

        private void SerializeId(BsonSerializationContext context, BsonValue id)
        {
            if (id != null)
            {
                context.Writer.WriteName("id");
                context.SerializeWithChildContext(BsonValueSerializer.Instance, id);
            }
        }

        private void SerializeProperties(BsonSerializationContext context, BsonDocument properties)
        {
            if (properties != null)
            {
                context.Writer.WriteName("properties");
                context.SerializeWithChildContext(BsonDocumentSerializer.Instance, properties);
            }
        }
    }
}
