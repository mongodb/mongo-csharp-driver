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
    /// Represents a serializer for a GeoJsonFeature value.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public class GeoJsonFeatureSerializer<TCoordinates> : BsonBaseSerializer<GeoJsonFeature<TCoordinates>> where TCoordinates : GeoJsonCoordinates
    {
        // private fields

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        public override GeoJsonFeature<TCoordinates> Deserialize(BsonDeserializationContext context)
        {
            var helper = new Helper();
            return (GeoJsonFeature<TCoordinates>)helper.Deserialize(context);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, GeoJsonFeature<TCoordinates> value)
        {
            var helper = new Helper();
            helper.Serialize(context, value);
        }

        // nested classes
        internal class Helper : GeoJsonObjectSerializer<TCoordinates>.Helper
        {
            // private fields
            private readonly IBsonSerializer<GeoJsonGeometry<TCoordinates>> _geometrySerializer = BsonSerializer.LookupSerializer<GeoJsonGeometry<TCoordinates>>();
            private GeoJsonGeometry<TCoordinates> _geometry;

            // constructors
            public Helper()
                : base(typeof(GeoJsonFeature<TCoordinates>), "Feature", new GeoJsonFeatureArgs<TCoordinates>())
            {
            }

            // public properties
            public GeoJsonFeatureArgs<TCoordinates> FeatureArgs
            {
                get { return (GeoJsonFeatureArgs<TCoordinates>)Args; }
            }

            public GeoJsonGeometry<TCoordinates> Geometry
            {
                get { return _geometry; }
                set { _geometry = value; }
            }

            // protected methods
            protected override GeoJsonObject<TCoordinates> CreateObject()
            {
                return new GeoJsonFeature<TCoordinates>(FeatureArgs, _geometry);
            }

            /// <summary>
            /// Deserializes a field.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="name">The name.</param>
            protected override void DeserializeField(BsonDeserializationContext context, string name)
            {
                switch (name)
                {
                    case "geometry": _geometry = DeserializeGeometry(context); break;
                    case "id": FeatureArgs.Id = DeserializeId(context); break;
                    case "properties": FeatureArgs.Properties = DeserializeProperties(context); break;
                    default: base.DeserializeField(context, name); break;
                }
            }

            /// <summary>
            /// Serializes the fields.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="obj">The GeoJson object.</param>
            protected override void SerializeFields(BsonSerializationContext context, GeoJsonObject<TCoordinates> obj)
            {
                base.SerializeFields(context, obj);
                var feature = (GeoJsonFeature<TCoordinates>)obj;
                SerializeGeometry(context, feature.Geometry);
                SerializeId(context, feature.Id);
                SerializeProperties(context, feature.Properties);
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
}
