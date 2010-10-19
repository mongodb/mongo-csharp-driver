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

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.DefaultSerializer {
    public class GenericArraySerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            VerifyNominalType(nominalType);
            var elementType = nominalType.GetElementType();
            var deserializeElementHelperDefinition = this.GetType().GetMethod("DeserializeElementHelper");
            var deserializeElementHelperInfo = deserializeElementHelperDefinition.MakeGenericMethod(elementType);
            var parameters = new object[] { bsonReader, null };
            var result = deserializeElementHelperInfo.Invoke(this, parameters);
            name = (string) parameters[1];
            return result;
        }

        public object DeserializeElementHelper<TElement>(
            BsonReader bsonReader,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else {
                bsonReader.ReadArrayName(out name);
                bsonReader.ReadStartDocument();
                List<TElement> value = new List<TElement>();
                while (bsonReader.HasElement()) {
                    string elementName; // element names are ignored on input
                    TElement element = BsonSerializer.DeserializeElement<TElement>(bsonReader, out elementName);
                    value.Add(element);
                }
                bsonReader.ReadEndDocument();
                return value.ToArray();
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            VerifyNominalType(nominalType);
            var elementType = nominalType.GetElementType();
            var serializeElementHelperDefinition = this.GetType().GetMethod("SerializeElementHelper");
            var serializeElementHelperInfo = serializeElementHelperDefinition.MakeGenericMethod(elementType);
            serializeElementHelperInfo.Invoke(this, new object[] { bsonWriter, name, value, useCompactRepresentation });
        }

        public void SerializeElementHelper<T>(
            BsonWriter bsonWriter,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                var array = (T[]) value;
                bsonWriter.WriteArrayName(name);
                bsonWriter.WriteStartDocument();
                for (int index = 0; index < array.Length; index++) {
                    var elementName = index.ToString();
                    var elementValue = array[index];
                    BsonSerializer.SerializeElement(bsonWriter, elementName, elementValue, useCompactRepresentation);
                }
                bsonWriter.WriteEndDocument();
            }
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
