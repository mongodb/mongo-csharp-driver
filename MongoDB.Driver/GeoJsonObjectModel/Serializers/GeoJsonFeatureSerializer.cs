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
    public class GeoJsonFeatureSerializer<TCoordinates> : GeoJsonObjectSerializer<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private readonly IBsonSerializer _geometrySerializer = BsonSerializer.LookupSerializer(typeof(GeoJsonGeometry<TCoordinates>));

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
            return DeserializeGeoJsonObject(bsonReader, new FeatureData());
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
            var featureData = (FeatureData)data;
            switch (name)
            {
                case "geometry": featureData.Geometry = DeserializeGeometry(bsonReader); break;
                case "id": featureData.FeatureArgs.Id = DeserializeId(bsonReader); break;
                case "properties": featureData.FeatureArgs.Properties = DeserializeProperties(bsonReader); break;
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
            var feature = (GeoJsonFeature<TCoordinates>)obj;
            SerializeGeometry(bsonWriter, feature.Geometry);
            SerializeId(bsonWriter, feature.Id);
            SerializeProperties(bsonWriter, feature.Properties);
        }

        // private methods
        private GeoJsonGeometry<TCoordinates> DeserializeGeometry(BsonReader bsonReader)
        {
            return (GeoJsonGeometry<TCoordinates>)_geometrySerializer.Deserialize(bsonReader, typeof(TCoordinates), null);
        }

        private BsonValue DeserializeId(BsonReader bsonReader)
        {
            return (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null);
        }

        private BsonDocument DeserializeProperties(BsonReader bsonReader)
        {
            return (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
        }

        private void SerializeGeometry(BsonWriter bsonWriter, GeoJsonGeometry<TCoordinates> geometry)
        {
            bsonWriter.WriteName("geometry");
            _geometrySerializer.Serialize(bsonWriter, typeof(GeoJsonGeometry<TCoordinates>), geometry, null);
        }

        private void SerializeId(BsonWriter bsonWriter, BsonValue id)
        {
            if (id != null)
            {
                bsonWriter.WriteName("id");
                BsonValueSerializer.Instance.Serialize(bsonWriter, typeof(BsonValue), id, null);
            }
        }

        private void SerializeProperties(BsonWriter bsonWriter, BsonDocument properties)
        {
            if (properties != null)
            {
                bsonWriter.WriteName("properties");
                BsonDocumentSerializer.Instance.Serialize(bsonWriter, typeof(BsonDocument), properties, null);
            }
        }

        // nested classes
        private class FeatureData : ObjectData
        {
            // private fields
            private GeoJsonGeometry<TCoordinates> _geometry;

            // constructors
            public FeatureData()
                : base(new GeoJsonFeatureArgs<TCoordinates>(), "Feature")
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

            // public methods
            public override object CreateInstance()
            {
                return new GeoJsonFeature<TCoordinates>(FeatureArgs, _geometry);
            }
        }
    }
}
