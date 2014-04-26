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
    /// Represents a serializer for a GeoJson object.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public class GeoJsonObjectSerializer<TCoordinates> : BsonBaseSerializer<GeoJsonObject<TCoordinates>> where TCoordinates : GeoJsonCoordinates
    {
        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        public override GeoJsonObject<TCoordinates> Deserialize(BsonDeserializationContext context)
        {
            var helper = new Helper();
            return helper.Deserialize(context);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, GeoJsonObject<TCoordinates> value)
        {
            var helper = new Helper();
            helper.Serialize(context, value);
        }

        // nested types
        /// <summary>
        /// Represents data being collected during serialization to create an instance of a GeoJsonObject.
        /// </summary>
        internal class Helper
        {
            // private fields
            private readonly Type _objectType;
            private readonly string _expectedDiscriminator;
            private readonly GeoJsonObjectArgs<TCoordinates> _args;
            private readonly IBsonSerializer<GeoJsonBoundingBox<TCoordinates>> _boundingBoxSerializer = BsonSerializer.LookupSerializer<GeoJsonBoundingBox<TCoordinates>>();
            private readonly IBsonSerializer<GeoJsonCoordinateReferenceSystem> _coordinateReferenceSystemSerializer = BsonSerializer.LookupSerializer<GeoJsonCoordinateReferenceSystem>();

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="Helper" /> class.
            /// </summary>
            public Helper()
                : this(typeof(GeoJsonObject<TCoordinates>), null, null)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Helper" /> class.
            /// </summary>
            /// <param name="objectType">The object type.</param>
            /// <param name="expectedDiscriminator">The expected discriminator.</param>
            /// <param name="args">The args.</param>
            protected Helper(Type objectType, string expectedDiscriminator, GeoJsonObjectArgs<TCoordinates> args)
            {
                _objectType = objectType;
                _expectedDiscriminator = expectedDiscriminator;
                _args = args;
            }

            // public properties
            public GeoJsonObjectArgs<TCoordinates> Args
            {
                get { return _args; }
            }

            // public methods
            /// <summary>
            /// Deserializes a value.
            /// </summary>
            /// <param name="context">The deserialization context.</param>
            /// <returns>The value.</returns>
            public GeoJsonObject<TCoordinates> Deserialize(BsonDeserializationContext context)
            {
                var bsonReader = context.Reader;

                if (bsonReader.GetCurrentBsonType() == BsonType.Null)
                {
                    bsonReader.ReadNull();
                    return null;
                }
                else
                {
                    var actualType = GetActualType(bsonReader);
                    if (actualType == _objectType)
                    {
                        return DeserializeObject(context);
                    }
                    else
                    {
                        var serializer = BsonSerializer.LookupSerializer(actualType);
                        return (GeoJsonObject<TCoordinates>)serializer.Deserialize(context);
                    }
                }
            }

            /// <summary>
            /// Serializes a value.
            /// </summary>
            /// <param name="context">The serialization context.</param>
            /// <param name="value">The value.</param>
            public void Serialize(BsonSerializationContext context, GeoJsonObject<TCoordinates> value)
            {
                var bsonWriter = context.Writer;

                if (value == null)
                {
                    bsonWriter.WriteNull();
                }
                else
                {
                    var actualType = value.GetType();
                    if (actualType == _objectType)
                    {
                        SerializeObject(context, value);
                    }
                    else
                    {
                        var serializer = BsonSerializer.LookupSerializer(actualType);
                        serializer.Serialize(context, value);
                    }
                }
            }

            // protected methods
            /// <summary>
            /// Creates the object.
            /// </summary>
            /// <returns>An instance of a GeoJsonObject.</returns>
            protected virtual GeoJsonObject<TCoordinates> CreateObject()
            {
                throw new NotSupportedException("Cannot create an abstract GeoJsonObject.");
            }

            /// <summary>
            /// Deserializes a field.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="name">The name.</param>
            protected virtual void DeserializeField(BsonDeserializationContext context, string name)
            {
                switch (name)
                {
                    case "type": DeserializeDiscriminator(context, _expectedDiscriminator); break;
                    case "bbox": _args.BoundingBox = DeserializeBoundingBox(context); break;
                    case "crs": _args.CoordinateReferenceSystem = DeserializeCoordinateReferenceSystem(context); break;
                    default: DeserializeExtraMember(context, name); break;
                }
            }

            /// <summary>
            /// Serializes the fields.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="obj">The GeoJson object.</param>
            protected virtual void SerializeFields(BsonSerializationContext context, GeoJsonObject<TCoordinates> obj)
            {
                SerializeDiscriminator(context, obj.Type);
                SerializeCoordinateReferenceSystem(context, obj.CoordinateReferenceSystem);
                SerializeBoundingBox(context, obj.BoundingBox);
            }

            // private methods
            private GeoJsonBoundingBox<TCoordinates> DeserializeBoundingBox(BsonDeserializationContext context)
            {
                return context.DeserializeWithChildContext(_boundingBoxSerializer);
            }

            private GeoJsonCoordinateReferenceSystem DeserializeCoordinateReferenceSystem(BsonDeserializationContext context)
            {
                return context.DeserializeWithChildContext(_coordinateReferenceSystemSerializer);
            }

            private void DeserializeDiscriminator(BsonDeserializationContext context, string expectedDiscriminator)
            {
                var discriminator = context.Reader.ReadString();
                if (discriminator != expectedDiscriminator)
                {
                    var message = string.Format("Type '{0}' does not match expected type '{1}'.", discriminator, expectedDiscriminator);
                    throw new FormatException(message);
                }
            }

            private void DeserializeExtraMember(BsonDeserializationContext context, string name)
            {
                var value = context.DeserializeWithChildContext(BsonValueSerializer.Instance);
                if (_args.ExtraMembers == null)
                {
                    _args.ExtraMembers = new BsonDocument();
                }
                _args.ExtraMembers[name] = value;
            }

            private GeoJsonObject<TCoordinates> DeserializeObject(BsonDeserializationContext context)
            {
                var bsonReader = context.Reader;

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
                        DeserializeField(context, name);
                    }
                    bsonReader.ReadEndDocument();

                    return CreateObject();
                }
            }

            private Type GetActualType(BsonReader bsonReader)
            {
                var bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                if (bsonReader.FindElement("type"))
                {
                    var discriminator = bsonReader.ReadString();
                    bsonReader.ReturnToBookmark(bookmark);

                    switch (discriminator)
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
                            var message = string.Format("The type field of the GeoJsonGeometry is not valid: '{0}'.", discriminator);
                            throw new FormatException(message);
                    }
                }
                else
                {
                    throw new FormatException("GeoJson object is missing the type field.");
                }
            }

            private void SerializeBoundingBox(BsonSerializationContext context, GeoJsonBoundingBox<TCoordinates> boundingBox)
            {
                if (boundingBox != null)
                {
                    context.Writer.WriteName("bbox");
                    context.SerializeWithChildContext(_boundingBoxSerializer, boundingBox);
                }
            }

            private void SerializeCoordinateReferenceSystem(BsonSerializationContext context, GeoJsonCoordinateReferenceSystem coordinateReferenceSystem)
            {
                if (coordinateReferenceSystem != null)
                {
                    context.Writer.WriteName("crs");
                    context.SerializeWithChildContext(_coordinateReferenceSystemSerializer, coordinateReferenceSystem);
                }
            }

            private void SerializeDiscriminator(BsonSerializationContext context, GeoJsonObjectType type)
            {
                context.Writer.WriteString("type", type.ToString());
            }

            private void SerializeExtraMembers(BsonSerializationContext context, BsonDocument extraMembers)
            {
                if (extraMembers != null)
                {
                    var bsonWriter = context.Writer;
                    foreach (var element in extraMembers)
                    {
                        bsonWriter.WriteName(element.Name);
                        context.SerializeWithChildContext(BsonValueSerializer.Instance, element.Value);
                    }
                }
            }

            private void SerializeObject(BsonSerializationContext context, GeoJsonObject<TCoordinates> obj)
            {
                var bsonWriter = context.Writer;

                bsonWriter.WriteStartDocument();
                SerializeFields(context, obj);
                SerializeExtraMembers(context, obj.ExtraMembers);
                bsonWriter.WriteEndDocument();
            }
        }
    }
}
