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

namespace MongoDB.BsonLibrary.Serialization {
    public class BsonClassMapSerializer : IBsonSerializer {
        #region private static fields
        private static BsonClassMapSerializer singleton = new BsonClassMapSerializer();
        #endregion

        #region constructors
        private BsonClassMapSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonClassMapSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public object Deserialize(
            BsonReader bsonReader,
            Type classType
        ) {
            var classMap = BsonClassMap.LookupClassMap(classType);

            // CreateObject peeks at the discriminator if necessary and might return an instance of a subclass of classType
            var obj = classMap.CreateObject(bsonReader);
            if (obj.GetType() != classType) {
                // since an instance of a subclass was created set classType and classMap to the subclass
                classType = obj.GetType();
                classMap = BsonClassMap.LookupClassMap(classType);
            }

            var missingElementPropertyMaps = new List<BsonPropertyMap>(classMap.PropertyMaps); // make a copy!
            bsonReader.ReadStartDocument();
            BsonType bsonType;
            string elementName;
            while (bsonReader.HasElement(out bsonType, out elementName)) {
                var propertyMap = classMap.GetPropertyMapForElement(elementName);
                if (propertyMap != null) {
                    propertyMap.PropertySerializer.DeserializeProperty(bsonReader, obj, propertyMap);
                    missingElementPropertyMaps.Remove(propertyMap);
                } else {
                    // TODO: how to handle extra elements?
                    throw new BsonSerializationException("Unexpected element");
                }
            }
            bsonReader.ReadEndDocument();

            foreach (var propertyMap in missingElementPropertyMaps) {
                if (propertyMap.IsRequired) {
                    string message = string.Format("Required element is missing: {0}", propertyMap.ElementName);
                    throw new BsonSerializationException(message);
                }

                if (propertyMap.HasDefaultValue) {
                    propertyMap.ApplyDefaultValue(obj);
                }
            }

            return obj;
        }

        public void Serialize(
            BsonWriter bsonWriter,
            object obj,
            bool serializeIdFirst
        ) {
            var classMap = BsonClassMap.LookupClassMap(obj.GetType());

            bsonWriter.WriteStartDocument();
            BsonPropertyMap idPropertyMap = null;
            if (serializeIdFirst) {
                idPropertyMap = classMap.IdPropertyMap;
                if (idPropertyMap != null) {
                    idPropertyMap.PropertySerializer.SerializeProperty(bsonWriter, obj, idPropertyMap);
                }
            }

            foreach (var propertyMap in classMap.PropertyMaps) {
                // note: if serializeIdFirst is false then idPropertyMap will be null (so no property will be skipped)
                if (propertyMap != idPropertyMap) {
                    propertyMap.PropertySerializer.SerializeProperty(bsonWriter, obj, propertyMap);
                }
            }
            bsonWriter.WriteEndDocument();
        }
        #endregion
    }
}
