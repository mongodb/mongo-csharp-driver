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
    public class GeoJsonLinkedCoordinateReferenceSystemSerializer : GeoJsonCoordinateReferenceSystemSerializer
    {
        // public methods
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                bsonReader.ReadStartDocument();
                DeserializeType(bsonReader, "link");
                bsonReader.ReadName("properties");
                bsonReader.ReadStartDocument();
                var href = bsonReader.ReadString("href");
                string hrefType = null;
                if (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    hrefType = bsonReader.ReadString("type");
                }
                bsonReader.ReadEndDocument();
                bsonReader.ReadEndDocument();

                return new GeoJsonLinkedCoordinateReferenceSystem(href, hrefType);
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
                var crs = (GeoJsonLinkedCoordinateReferenceSystem)value;

                bsonWriter.WriteStartDocument();
                SerializeType(bsonWriter, crs.Type);
                bsonWriter.WriteStartDocument("properties");
                bsonWriter.WriteString("href", crs.HRef);
                if (crs.HRefType != null)
                {
                    bsonWriter.WriteString("type", crs.HRefType);
                }
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }
        }
    }
}
