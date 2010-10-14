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

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class DefaultPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static DefaultPropertySerializer singleton = new DefaultPropertySerializer();
        #endregion

        #region constructors
        private DefaultPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static DefaultPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var propertyType = propertyMap.PropertyInfo.PropertyType;
            var genericDeserializePropertyHelperInfo = this.GetType().GetMethod("DeserializePropertyHelper");
            var deserializePropertyHelperInfo = genericDeserializePropertyHelperInfo.MakeGenericMethod(propertyType);
            deserializePropertyHelperInfo.Invoke(this, new object[] { bsonReader, obj, propertyMap });
        }

        public void DeserializePropertyHelper<T>(
           BsonReader bsonReader,
           object obj,
           BsonPropertyMap propertyMap
       ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            T value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = default(T);
            } else {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                value = BsonSerializer.Deserialize<T>(bsonReader);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var propertyType = propertyMap.PropertyInfo.PropertyType;
            var genericSerializePropertyHelperInfo = this.GetType().GetMethod("SerializePropertyHelper");
            var serializePropertyHelperInfo = genericSerializePropertyHelperInfo.MakeGenericMethod(propertyType);
            serializePropertyHelperInfo.Invoke(this, new object[] { bsonWriter, obj, propertyMap });
        }

        public void SerializePropertyHelper<T>(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (T) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                BsonSerializer.Serialize<T>(bsonWriter, value);
            }
        }
        #endregion
    }
}
