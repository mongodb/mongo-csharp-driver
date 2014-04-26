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
    /// Represents a serializer for a GeoJsonFeatureCollection value.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public class GeoJsonFeatureCollectionSerializer<TCoordinates> : BsonBaseSerializer<GeoJsonFeatureCollection<TCoordinates>> where TCoordinates : GeoJsonCoordinates
    {
        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        public override GeoJsonFeatureCollection<TCoordinates> Deserialize(BsonDeserializationContext context)
        {
            var helper = new Helper();
            return (GeoJsonFeatureCollection<TCoordinates>)helper.Deserialize(context);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, GeoJsonFeatureCollection<TCoordinates> value)
        {
            var helper = new Helper();
            helper.Serialize(context, value);
        }

        // nested classes
        internal class Helper : GeoJsonObjectSerializer<TCoordinates>.Helper
        {
            // private fields
            private readonly IBsonSerializer<GeoJsonFeature<TCoordinates>> _featureSerializer = BsonSerializer.LookupSerializer<GeoJsonFeature<TCoordinates>>();
            private List<GeoJsonFeature<TCoordinates>> _features;

            // constructors
            public Helper()
                : base(typeof(GeoJsonFeatureCollection<TCoordinates>), "FeatureCollection", new GeoJsonObjectArgs<TCoordinates>())
            {
            }

            // public properties
            public List<GeoJsonFeature<TCoordinates>> Features
            {
                get { return _features; }
                set { _features = value; }
            }

            // protected methods
            protected override GeoJsonObject<TCoordinates> CreateObject()
            {
                return new GeoJsonFeatureCollection<TCoordinates>(Args, _features);
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
                    case "features": _features = DeserializeFeatures(context); break;
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
                var featureCollection = (GeoJsonFeatureCollection<TCoordinates>)obj;
                SerializeFeatures(context, featureCollection.Features);
            }

            // private methods
            private List<GeoJsonFeature<TCoordinates>> DeserializeFeatures(BsonDeserializationContext context)
            {
                var bsonReader = context.Reader;

                bsonReader.ReadStartArray();
                var features = new List<GeoJsonFeature<TCoordinates>>();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var feature = context.DeserializeWithChildContext(_featureSerializer);
                    features.Add(feature);
                }
                bsonReader.ReadEndArray();

                return features;
            }

            private void SerializeFeatures(BsonSerializationContext context, IEnumerable<GeoJsonFeature<TCoordinates>> features)
            {
                var bsonWriter = context.Writer;

                bsonWriter.WriteName("features");
                bsonWriter.WriteStartArray();
                foreach (var feature in features)
                {
                    context.SerializeWithChildContext(_featureSerializer, feature);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }
}
