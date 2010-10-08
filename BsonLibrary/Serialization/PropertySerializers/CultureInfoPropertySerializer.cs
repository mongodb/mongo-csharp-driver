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
using System.Globalization;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class CultureInfoPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static CultureInfoPropertySerializer singleton = new CultureInfoPropertySerializer();
        #endregion

        #region constructors
        private CultureInfoPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static CultureInfoPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(CultureInfo); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            bsonReader.ReadDocumentName(propertyMap.ElementName);
            bsonReader.ReadStartDocument();
            bsonReader.VerifyString("_t", typeof(CultureInfo).FullName);
            var value = new CultureInfo(bsonReader.ReadString("v"));
            bsonReader.ReadEndDocument();
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (CultureInfo) propertyMap.Getter(obj);
            bsonWriter.WriteDocumentName(propertyMap.ElementName);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", typeof(CultureInfo).FullName);
            bsonWriter.WriteString("v", value.ToString());
            bsonWriter.WriteEndDocument();
        }
        #endregion
    }
}
