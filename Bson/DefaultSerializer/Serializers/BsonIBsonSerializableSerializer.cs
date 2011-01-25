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
    public class BsonIBsonSerializableSerializer : IBsonSerializer {
        #region private static fields
        private static BsonIBsonSerializableSerializer instance = new BsonIBsonSerializableSerializer();
        #endregion

        #region constructors
        public BsonIBsonSerializableSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonIBsonSerializableSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(IBsonSerializable), instance);
        }
        #endregion

        #region public methods
        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var value = (IBsonSerializable) Activator.CreateInstance(nominalType, true); // private default constructor OK
            return value.Deserialize(bsonReader, nominalType, options);
        }

        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            var value = (IBsonSerializable) Activator.CreateInstance(actualType, true); // private default constructor OK
            return value.Deserialize(bsonReader, nominalType, options);
        }

        public bool GetDocumentId(
            object document,
            out object id,
            out IIdGenerator idGenerator
        ) {
            var bsonSerializable = (IBsonSerializable) document;
            return bsonSerializable.GetDocumentId(out id, out idGenerator);
        }

        public void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var serializable = (IBsonSerializable) value;
                serializable.Serialize(bsonWriter, nominalType, options);
            }
        }

        public void SetDocumentId(
            object document,
            object id
        ) {
            var bsonSerializable = (IBsonSerializable) document;
            bsonSerializable.SetDocumentId(id);
        }
        #endregion
    }
}
