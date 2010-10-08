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

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class ObjectIdPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static ObjectIdPropertySerializer singleton = new ObjectIdPropertySerializer();
        #endregion

        #region constructors
        private ObjectIdPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static ObjectIdPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(ObjectId); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            int timestamp;
            long machinePidIncrement;
            bsonReader.ReadObjectId(propertyMap.ElementName, out timestamp, out machinePidIncrement);
            var value = new ObjectId(timestamp, machinePidIncrement);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (ObjectId) propertyMap.Getter(obj);
            bsonWriter.WriteObjectId(propertyMap.ElementName, value.Timestamp, value.MachinePidIncrement);
        }
        #endregion
    }
}
