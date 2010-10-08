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
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class DateTimeOffsetPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static DateTimeOffsetPropertySerializer singleton = new DateTimeOffsetPropertySerializer();
        #endregion

        #region constructors
        private DateTimeOffsetPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static DateTimeOffsetPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(DateTimeOffset); }
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
            bsonReader.VerifyString("_t", typeof(DateTimeOffset).FullName);
            var dateTime = DateTime.Parse(bsonReader.ReadString("dt")); // Kind = DateTimeKind.Unspecified
            var offset = TimeSpan.Parse(bsonReader.ReadString("o"));
            bsonReader.ReadEndDocument();
            var value = new DateTimeOffset(dateTime, offset);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            // note: the DateTime portion has to be serialized as a string because it is NOT in UTC
            var value = (DateTimeOffset) propertyMap.Getter(obj);
            bsonWriter.WriteDocumentName(propertyMap.ElementName);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", typeof(DateTimeOffset).FullName);
            bsonWriter.WriteString("dt", value.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF")); // omit trailing zeros
            bsonWriter.WriteString("o", Regex.Replace(value.Offset.ToString(), ":00$", "")); // omit trailing zeros
            bsonWriter.WriteEndDocument();
        }
        #endregion
    }
}
