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
using System.IO;
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
            // peek at the discriminator (if present) to see what class to create an instance for
            var discriminator = bsonReader.FindString("_t");
            if (discriminator != null) {
                var actualType = Type.GetType(discriminator);
                if (!classType.IsAssignableFrom(actualType)) {
                    string message = string.Format("Actual type {0} is not assignable to expected type {1}", actualType.FullName, classType.FullName);
                    throw new FileFormatException(message);
                }
                classType = actualType;
            }
            var classMap = BsonClassMap.LookupClassMap(classType);
            if (classMap.IsAnonymous) {
                throw new InvalidOperationException("Anonymous classes cannot be deserialized");
            }
            var obj = Activator.CreateInstance(classType);

            var missingElementPropertyMaps = new List<BsonPropertyMap>(classMap.PropertyMaps); // make a copy!
            bsonReader.ReadStartDocument();
            BsonType bsonType;
            string elementName;
            while (bsonReader.HasElement(out bsonType, out elementName)) {
                if (elementName == "_t") {
                    bsonReader.ReadString("_t"); // skip over discriminator
                    continue;
                }

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
            bool serializeIdFirst,
            bool serializeDiscriminator
        ) {
            var objType = obj.GetType();
            var classMap = BsonClassMap.LookupClassMap(objType);

            bsonWriter.WriteStartDocument();
            BsonPropertyMap idPropertyMap = null;
            if (serializeIdFirst) {
                idPropertyMap = classMap.IdPropertyMap;
                if (idPropertyMap != null) {
                    idPropertyMap.PropertySerializer.SerializeProperty(bsonWriter, obj, idPropertyMap);
                }
            }

            if (serializeDiscriminator) {
                var discriminator = string.Join(",", objType.AssemblyQualifiedName.Split(','), 0, 2);
                bsonWriter.WriteString("_t", discriminator);
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
