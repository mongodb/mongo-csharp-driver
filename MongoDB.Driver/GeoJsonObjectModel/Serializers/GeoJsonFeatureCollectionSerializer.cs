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

namespace MongoDB.Driver.GeoJsonObjectModel.Serializers
{
    /// <summary>
    /// Represents a serializer for a GeoJsonFeatureCollection value.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public class GeoJsonFeatureCollectionSerializer<TCoordinates> : GeoJsonObjectSerializer<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private readonly IBsonSerializer _featureSerializer = BsonSerializer.LookupSerializer(typeof(GeoJsonFeature<TCoordinates>));

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
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            return DeserializeGeoJsonObject(bsonReader, new FeatureCollectionData());
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
            SerializeGeoJsonObject(bsonWriter, (GeoJsonObject<TCoordinates>)value);
        }

        // protected methods
        /// <summary>
        /// Deserializes a field.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="name">The name.</param>
        /// <param name="data">The data.</param>
        protected override void DeserializeField(BsonReader bsonReader, string name, ObjectData data)
        {
            var featureCollectionData = (FeatureCollectionData)data;
            switch (name)
            {
                case "features": featureCollectionData.Features = DeserializeFeatures(bsonReader); break;
                default: base.DeserializeField(bsonReader, name, data); break;
            }
        }

        /// <summary>
        /// Serializes the fields.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="obj">The GeoJson object.</param>
        protected override void SerializeFields(BsonWriter bsonWriter, GeoJsonObject<TCoordinates> obj)
        {
            var featureCollection = (GeoJsonFeatureCollection<TCoordinates>)obj;
            SerializeFeatures(bsonWriter, featureCollection.Features);
        }

        // private methods
        private List<GeoJsonFeature<TCoordinates>> DeserializeFeatures(BsonReader bsonReader)
        {
            var features = new List<GeoJsonFeature<TCoordinates>>();

            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var feature = (GeoJsonFeature<TCoordinates>)_featureSerializer.Deserialize(bsonReader, typeof(GeoJsonFeature<TCoordinates>), null);
                features.Add(feature);
            }
            bsonReader.ReadEndArray();

            return features;
        }

        private void SerializeFeatures(BsonWriter bsonWriter, IEnumerable<GeoJsonFeature<TCoordinates>> features)
        {
            bsonWriter.WriteName("features");
            bsonWriter.WriteStartArray();
            foreach (var feature in features)
            {
                _featureSerializer.Serialize(bsonWriter, typeof(GeoJsonFeature<TCoordinates>), feature, null);
            }
            bsonWriter.WriteEndArray();
        }

        // nested classes
        private class FeatureCollectionData : ObjectData
        {
            // private fields
            private List<GeoJsonFeature<TCoordinates>> _features;

            // constructors
            public FeatureCollectionData()
                : base("FeatureCollection")
            {
            }

            // public properties
            public List<GeoJsonFeature<TCoordinates>> Features
            {
                get { return _features; }
                set { _features = value; }
            }

            // public methods
            public override object CreateInstance()
            {
                return new GeoJsonFeatureCollection<TCoordinates>(Args, _features);
            }
        }
    }
}
