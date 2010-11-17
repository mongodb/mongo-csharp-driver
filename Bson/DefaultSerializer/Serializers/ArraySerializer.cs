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
}
