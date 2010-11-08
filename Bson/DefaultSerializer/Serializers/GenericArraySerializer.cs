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
    public class GenericArraySerializer : BsonBaseSerializer {
        #region private static fields
        private static GenericArraySerializer singleton = new GenericArraySerializer();
        #endregion

        #region constructors
        private GenericArraySerializer() {
        }
        #endregion

        #region public static properties
        public static GenericArraySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            VerifyNominalType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var elementType = nominalType.GetElementType();
                var deserializeHelperDefinition = this.GetType().GetMethod("DeserializeHelper");
                var deserializeHelperInfo = deserializeHelperDefinition.MakeGenericMethod(elementType);
                var parameters = new object[] { bsonReader };
                var result = deserializeHelperInfo.Invoke(this, parameters);
                return result;
            }
        }

        public object DeserializeHelper<TElement>(
            BsonReader bsonReader
        ) {
            bsonReader.ReadStartArray();
            List<TElement> value = new List<TElement>();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                bsonReader.SkipName();
                TElement element = BsonSerializer.Deserialize<TElement>(bsonReader);
                value.Add(element);
            }
            bsonReader.ReadEndArray();
            return value.ToArray();
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            VerifyNominalType(nominalType);
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var elementType = nominalType.GetElementType();
                var serializeHelperDefinition = this.GetType().GetMethod("SerializeHelper");
                var serializeHelperInfo = serializeHelperDefinition.MakeGenericMethod(elementType);
                serializeHelperInfo.Invoke(this, new object[] { bsonWriter, value });
            }
        }

        public void SerializeHelper<TElement>(
            BsonWriter bsonWriter,
            TElement[] value
        ) {
            bsonWriter.WriteStartArray();
            for (int index = 0; index < value.Length; index++) {
                bsonWriter.WriteName(index.ToString());
                BsonSerializer.Serialize(bsonWriter, typeof(TElement), value[index]);
            }
            bsonWriter.WriteEndArray();
        }
        #endregion

        #region private methods
        private void VerifyNominalType(
            Type nominalType
        ) {
            if (!nominalType.IsArray) {
                var message = string.Format("GenericArraySerializer cannot be used with type: {0}", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
