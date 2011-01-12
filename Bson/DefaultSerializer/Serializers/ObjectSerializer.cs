/* Copyright 2010 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class ObjectSerializer : BsonBaseSerializer {
        #region private static fields
        private static ObjectSerializer instance = new ObjectSerializer();
        #endregion

        #region constructors
        public ObjectSerializer() {
        }
        #endregion

        #region public static properties
        public static ObjectSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(object), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            if (nominalType != typeof(object)) {
                var message = string.Format("ObjectSerializer called for nominal type: {0}", nominalType.FullName);
                throw new InvalidOperationException(message);
            }

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Document) {
                var bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                switch (bsonReader.ReadBsonType()) {
                    case BsonType.EndOfDocument:
                        bsonReader.ReadEndDocument();
                        return new object();
                    default:
                        bsonReader.ReturnToBookmark(bookmark);
                        var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(object));
                        var actualType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                        if (actualType == typeof(object)) {
                            throw new BsonSerializationException("Unable to determine actual type of document to deserialize");
                        }
                        var serializer = BsonSerializer.LookupSerializer(actualType);
                        return serializer.Deserialize(bsonReader, nominalType, actualType, null);
                }
            } else {
                var message = string.Format("Cannot deserialize an object from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var actualType = value.GetType();
                if (actualType != typeof(object)) {
                    var message = string.Format("ObjectSerializer called for type: {0}", actualType.FullName);
                    throw new InvalidOperationException(message);
                }

                bsonWriter.WriteStartDocument();
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }
}
