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
    public class ArraySerializer<T> : BsonBaseSerializer {
        #region constructors
        public ArraySerializer() {
        }

        public ArraySerializer(
            object serializationOptions
        ) {
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            VerifyType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadStartArray();
                List<T> list = new List<T>();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    bsonReader.SkipName();
                    var element = BsonSerializer.Deserialize<T>(bsonReader);
                    list.Add(element);
                }
                bsonReader.ReadEndArray();
                return list.ToArray();
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                VerifyType(value.GetType());
                var array = (T[]) value;
                bsonWriter.WriteStartArray();
                for (int index = 0; index < array.Length; index++) {
                    bsonWriter.WriteName(index.ToString());
                    BsonSerializer.Serialize(bsonWriter, typeof(T), array[index]);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion

        #region private methods
        private void VerifyType(
            Type type
        ) {
            if (type != typeof(T[])) {
                var message = string.Format("ArraySerializer<{0}> cannot be used with type: {1}", typeof(T).FullName, type.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class TwoDimensionalArraySerializer<T> : BsonBaseSerializer {
        #region constructors
        public TwoDimensionalArraySerializer() {
        }

        public TwoDimensionalArraySerializer(
            object serializationOptions
        ) {
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            VerifyType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadStartArray();
                var outerList = new List<List<T>>();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    bsonReader.SkipName();
                    bsonReader.ReadStartArray();
                    var innerList = new List<T>();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                        bsonReader.SkipName();
                        var element = BsonSerializer.Deserialize<T>(bsonReader);
                        innerList.Add(element);
                    }
                    bsonReader.ReadEndArray();
                    outerList.Add(innerList);
                }
                bsonReader.ReadEndArray();

                var length1 = outerList.Count;
                var length2 = (length1 == 0) ? 0 : outerList[0].Count;
                var array = new T[length1, length2];
                for (int i = 0; i < length1; i++) {
                    var innerList = outerList[i];
                    if (innerList.Count != length2) {
                        var message = string.Format("Inner list {0} is of wrong length: {1} (should be: {2})", i, innerList.Count, length2);
                        throw new FileFormatException(message);
                    }
                    for (int j = 0; j < length2; j++) {
                        array[i, j] = innerList[j];
                    }
                }

                return array;
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                VerifyType(value.GetType());
                var array = (T[,]) value;
                bsonWriter.WriteStartArray();
                var length1 = array.GetLength(0);
                var length2 = array.GetLength(1);
                for (int i = 0; i < length1; i++) {
                    bsonWriter.WriteStartArray(i.ToString());
                    for (int j = 0; j < length2; j++) {
                        bsonWriter.WriteName(j.ToString());
                        BsonSerializer.Serialize(bsonWriter, typeof(T), array[i, j]);
                    }
                    bsonWriter.WriteEndArray();
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion

        #region private methods
        private void VerifyType(
            Type type
        ) {
            if (type != typeof(T[,])) {
                var message = string.Format("TwoDimensionalArraySerializer<{0}> cannot be used with type: {1}", typeof(T).FullName, type.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
