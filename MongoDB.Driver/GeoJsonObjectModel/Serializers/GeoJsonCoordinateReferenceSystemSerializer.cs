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
    /// Represents a serializer for a GeoJsonCoordinateReferenceSystem value.
    /// </summary>
    public class GeoJsonCoordinateReferenceSystemSerializer : BsonBaseSerializer
    {
        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>
        /// An object.
        /// </returns>
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

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
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
        /// <summary>
        /// Deserializes the type of the coordinate reference system.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <exception cref="System.FormatException"></exception>
        protected void DeserializeType(BsonReader bsonReader, string expectedType)
        {
            var type = bsonReader.ReadString("type");
            if (type != expectedType)
            {
                var message = string.Format("Expected type to be '{0}'.", expectedType);
                throw new FormatException(message);
            }
        }

        /// <summary>
        /// Serializes the type of the coordinate reference system.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="type">The type.</param>
        protected void SerializeType(BsonWriter bsonWriter, string type)
        {
            bsonWriter.WriteString("type", type);
        }

        // private methods
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
                    case "link": return typeof(GeoJsonLinkedCoordinateReferenceSystem);
                    case "name": return typeof(GeoJsonNamedCoordinateReferenceSystem);
                    default:
                        var message = string.Format("The type field of the GeoJsonCoordinateReferenceSystem is not valid: '{0}'.", type);
                        throw new FormatException(message);
                }
            }
            else
            {
                throw new FormatException("GeoJsonCoordinateReferenceSystem object is missing the type field.");
            }
        }
    }
}
