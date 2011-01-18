/* Copyright 2010-2011 10gen Inc.
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
    public static class NullableSerializerRegistration {
        #region public static methods
        public static void RegisterGenericSerializerDefinitions() {
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(Nullable<>), typeof(NullableSerializer<>));
        }
        #endregion
    }

    public class NullableSerializer<T> : BsonBaseSerializer where T : struct {
        #region private fields
        private IBsonSerializer serializer;
        #endregion

        #region constructors
        public NullableSerializer() {
            serializer = BsonSerializer.LookupSerializer(typeof(T));
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return serializer.Deserialize(bsonReader, typeof(T), options);
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
                serializer.Serialize(bsonWriter, typeof(T), value, options);
            }
        }
        #endregion
    }
}
