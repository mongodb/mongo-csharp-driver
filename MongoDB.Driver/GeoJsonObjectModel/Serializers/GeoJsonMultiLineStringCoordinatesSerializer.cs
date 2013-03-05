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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel.Serializers
{
    public class GeoJsonMultiLineStringCoordinatesSerializer<TCoordinates> : BsonBaseSerializer where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private readonly IBsonSerializer _lineStringCoordinatesSerializer = BsonSerializer.LookupSerializer(typeof(GeoJsonLineStringCoordinates<TCoordinates>));

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
                var lineStrings = new List<GeoJsonLineStringCoordinates<TCoordinates>>();

                bsonReader.ReadStartArray();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var lineString = (GeoJsonLineStringCoordinates<TCoordinates>)_lineStringCoordinatesSerializer.Deserialize(bsonReader, typeof(TCoordinates), null);
                    lineStrings.Add(lineString);
                }
                bsonReader.ReadEndArray();

                return new GeoJsonMultiLineStringCoordinates<TCoordinates>(lineStrings);
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
                var lineStringCoordinates = (GeoJsonMultiLineStringCoordinates<TCoordinates>)value;

                bsonWriter.WriteStartArray();
                foreach (var lineString in lineStringCoordinates.LineStrings)
                {
                    _lineStringCoordinatesSerializer.Serialize(bsonWriter, typeof(TCoordinates), lineString, null);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }
}
