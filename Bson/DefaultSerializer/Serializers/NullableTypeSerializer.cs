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
    public class NullableTypeSerializer : BsonBaseSerializer {
        #region private static fields
        private static NullableTypeSerializer singleton = new NullableTypeSerializer();
        #endregion

        #region constructors
        private NullableTypeSerializer() {
        }
        #endregion

        #region public static properties
        public static NullableTypeSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                Type underlyingType = Nullable.GetUnderlyingType(nominalType);
                return BsonSerializer.Deserialize(bsonReader, underlyingType);
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
                Type underlyingType = Nullable.GetUnderlyingType(nominalType);
                BsonSerializer.Serialize(bsonWriter, underlyingType, value, serializeIdFirst);
            }
        }
        #endregion
    }
}
