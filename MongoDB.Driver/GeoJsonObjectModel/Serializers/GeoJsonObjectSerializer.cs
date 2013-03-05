/* Copyright 2010-2013 10gen Inc.
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
    public class GeoJsonObjectSerializer<TCoordinates> : BsonBaseSerializer where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private readonly IBsonSerializer _boundingBoxSerializer = BsonSerializer.LookupSerializer(typeof(GeoJsonBoundingBox<TCoordinates>));
        private readonly IBsonSerializer _coordinateReferenceSystemSerialzier = BsonSerializer.LookupSerializer(typeof(GeoJsonCoordinateReferenceSystem));

        // public methods
        public override object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                var actualType = GetActualType(bsonReader);
                var actualTypeSerializer = BsonSerializer.LookupSerializer(actualType);
                return actualTypeSerializer.Deserialize(bsonReader, nominalType, actualType, options);
            }
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                var actualTypeSerializer = BsonSerializer.LookupSerializer(actualType);
                actualTypeSerializer.Serialize(bsonWriter, nominalType, value, options);
            }
        }

        // protected methods
        protected virtual void DeserializeField(BsonReader bsonReader, string name, ObjectData data)
        {
            switch (name)
            {
                case "type": DeserializeType(bsonReader, data.ExpectedType); break;
                case "bbox": data.Args.BoundingBox = DeserializeBoundingBox(bsonReader); break;
                case "crs": data.Args.CoordinateReferenceSystem = DeserializeCoordinateReferenceSystem(bsonReader); break;
                default: DeserializeExtraMember(bsonReader, name, data); break;
            }
        }

        protected object DeserializeGeoJsonObject(BsonReader bsonReader, ObjectData data)
        {
            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                bsonReader.ReadStartDocument();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var name = bsonReader.ReadName();
                    DeserializeField(bsonReader, name, data);
                }
                bsonReader.ReadEndDocument();

                return data.CreateInstance();
            }
        }

        protected virtual void SerializeFields(BsonWriter bsonWriter, GeoJsonObject<TCoordinates> obj)
        {
        }

        protected void SerializeGeoJsonObject(BsonWriter bsonWriter, GeoJsonObject<TCoordinates> obj)
        {
            if (obj == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                bsonWriter.WriteStartDocument();
                SerializeType(bsonWriter, obj.Type);
                SerializeCoordinateReferenceSystem(bsonWriter, obj.CoordinateReferenceSystem);
                SerializeBoundingBox(bsonWriter, obj.BoundingBox);
                SerializeFields(bsonWriter, obj);
                SerializeExtraMembers(bsonWriter, obj.ExtraMembers);
                bsonWriter.WriteEndDocument();
            }
        }

        // private methods
        private GeoJsonBoundingBox<TCoordinates> DeserializeBoundingBox(BsonReader bsonReader)
        {
            return (GeoJsonBoundingBox<TCoordinates>)_boundingBoxSerializer.Deserialize(bsonReader, typeof(GeoJsonBoundingBox<TCoordinates>), null);
        }

        private GeoJsonCoordinateReferenceSystem DeserializeCoordinateReferenceSystem(BsonReader bsonReader)
        {
            return (GeoJsonCoordinateReferenceSystem)_coordinateReferenceSystemSerialzier.Deserialize(bsonReader, typeof(GeoJsonCoordinateReferenceSystem), null);
        }

        private void DeserializeExtraMember(BsonReader bsonReader, string name, ObjectData data)
        {
            var value = (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null);
            if (data.Args.ExtraMembers == null)
            {
                data.Args.ExtraMembers = new BsonDocument();
            }
            data.Args.ExtraMembers[name] = value;
        }

        private void DeserializeType(BsonReader bsonReader, string expectedType)
        {
            var type = bsonReader.ReadString();
            if (type != expectedType)
            {
                var message = string.Format("Type '{0}' does not match expected type '{1}'.", type, expectedType);
                throw new FormatException(message);
            }
        }

        private Type GetActualType(BsonReader bsonReader)
        {
            var bookmark = bsonReader.GetBookmark();
            bsonReader.ReadStartDocument();
            if (bsonReader.FindElement("type"))
            {
                var type = bsonReader.ReadString();
                bsonReader.ReturnToBookmark(bookmark);

                switch (type)
                {
                    case "Feature": return typeof(GeoJsonFeature<TCoordinates>);
                    case "FeatureCollection": return typeof(GeoJsonFeatureCollection<TCoordinates>);
                    case "GeometryCollection": return typeof(GeoJsonGeometryCollection<TCoordinates>);
                    case "LineString": return typeof(GeoJsonLineString<TCoordinates>);
                    case "MultiLineString": return typeof(GeoJsonMultiLineString<TCoordinates>);
                    case "MultiPoint": return typeof(GeoJsonMultiPoint<TCoordinates>);
                    case "MultiPolygon": return typeof(GeoJsonMultiPolygon<TCoordinates>);
                    case "Point": return typeof(GeoJsonPoint<TCoordinates>);
                    case "Polygon": return typeof(GeoJsonPolygon<TCoordinates>);
                    default:
                        var message = string.Format("The type field of the GeoJosnGeometry is not valid: '{0}'.", type);
                        throw new FormatException(message);
                }
            }
            else
            {
                throw new FormatException("GeoJson object is missing the type field.");
            }
        }

        private void SerializeBoundingBox(BsonWriter bsonWriter, GeoJsonBoundingBox<TCoordinates> boundingBox)
        {
            if (boundingBox != null)
            {
                bsonWriter.WriteName("bbox");
                _boundingBoxSerializer.Serialize(bsonWriter, typeof(GeoJsonBoundingBox<TCoordinates>), boundingBox, null);
            }
        }

        private void SerializeCoordinateReferenceSystem(BsonWriter bsonWriter, GeoJsonCoordinateReferenceSystem coordinateReferenceSystem)
        {
            if (coordinateReferenceSystem != null)
            {
                bsonWriter.WriteName("crs");
                _coordinateReferenceSystemSerialzier.Serialize(bsonWriter, typeof(GeoJsonCoordinateReferenceSystem), coordinateReferenceSystem, null);
            }
        }

        private void SerializeExtraMembers(BsonWriter bsonWriter, BsonDocument extraMembers)
        {
            if (extraMembers != null)
            {
                foreach (var element in extraMembers)
                {
                    bsonWriter.WriteName(element.Name);
                    BsonValueSerializer.Instance.Serialize(bsonWriter, typeof(BsonValue), element.Value, null);
                }
            }
        }

        private void SerializeType(BsonWriter bsonWriter, GeoJsonObjectType type)
        {
            bsonWriter.WriteString("type", type.ToString());
        }

        // nested types
        protected abstract class ObjectData
        {
            // private fields
            private readonly GeoJsonObjectArgs<TCoordinates> _args;
            private readonly string _expectedType;

            // constructors
            public ObjectData(string expectedType)
                : this(new GeoJsonObjectArgs<TCoordinates>(), expectedType)
            {
            }

            public ObjectData(GeoJsonObjectArgs<TCoordinates> args, string expectedType)
            {
                _args = args;
                _expectedType = expectedType;
            }

            // public properties
            public GeoJsonObjectArgs<TCoordinates> Args
            {
                get { return _args; }
            }

            public string ExpectedType
            {
                get { return _expectedType; }
            }

            // public methods
            public abstract object CreateInstance();
        }
    }
}
