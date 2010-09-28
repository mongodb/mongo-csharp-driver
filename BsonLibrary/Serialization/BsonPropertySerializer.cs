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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization {
    public class BsonPropertySerializer : IBsonSerializer {
        #region private static fields
        private static BsonPropertySerializer singleton = new BsonPropertySerializer();
        #endregion

        #region constructors
        private BsonPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public object Deserialize(
            BsonReader bsonReader,
            Type type
        ) {
            // TODO: implement Deserialize
            throw new NotImplementedException();
        }

        public void Serialize(
            BsonWriter bsonWriter,
            object obj,
            bool serializeIdFirst
        ) {
            // TODO: optimize away some of the reflection overhead
            bsonWriter.WriteStartDocument();
            PropertyInfo idProperty = null;
            if (serializeIdFirst) {
                idProperty = obj.GetType().GetProperty("_id");
                if (idProperty != null) {
                    SerializeProperty(bsonWriter, obj, idProperty);
                }
            }
            foreach (var property in obj.GetType().GetProperties()) {
                // note: if serializeIdFirst is false then idProperty will be null (so no property will be skipped)
                if (property != idProperty) {
                    SerializeProperty(bsonWriter, obj, property);
                }
            }
            bsonWriter.WriteEndDocument();
        }
        #endregion

        #region private methods
        private bool IsAnonymousType(
            Type type
        ) {
            // TODO: figure out if this is a reliable test
            return type.Namespace == null;
        }

        private void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            PropertyInfo property
        ) {
            var getMethod = property.GetGetMethod();
            var setMethod = property.GetSetMethod();
            if (getMethod != null && (setMethod != null || IsAnonymousType(obj.GetType()))) {
                string name = property.Name;
                object value = getMethod.Invoke(obj, new object[] { });
                BsonValue bsonValue;
                if (BsonTypeMapper.TryMapToBsonValue(value, out bsonValue)) {
                    // create a temporary BsonElement because it has the WriteTo method we need
                    new BsonElement(name, bsonValue).WriteTo(bsonWriter);
                } else {
                    throw new BsonException("BsonPropertySerializer doesn't know how to serializer type: {0}", value.GetType().FullName);
                }
            }
        }
        #endregion
    }
}
