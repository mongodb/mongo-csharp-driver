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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            VerifyNominalType(nominalType);

            bsonReader.ReadStartDocument();
            var discriminator = PeekDocumentDiscriminator(bsonReader); // returns null if no discriminator found
            var actualType = BsonClassMap.LookupActualType(nominalType, discriminator);
            var classMap = BsonClassMap.LookupClassMap(actualType);
            if (classMap.IsAnonymous) {
                throw new InvalidOperationException("Anonymous classes cannot be deserialized");
            }
            var obj = Activator.CreateInstance(actualType);

            var missingElementPropertyMaps = new List<BsonMemberMap>(classMap.MemberMaps); // make a copy!
            BsonType bsonType;
            string elementName;
            while (bsonReader.HasElement(out bsonType, out elementName)) {
                if (elementName == "_t") {
                    bsonReader.SkipElement("_t"); // skip over discriminator
                    continue;
                }

                var propertyMap = classMap.GetMemberMapForElement(elementName);
                if (propertyMap != null) {
                    var elementDiscriminator = PeekElementDiscriminator(bsonReader, bsonType, elementName); // returns null if no discriminator found
                    var actualElementType = BsonClassMap.LookupActualType(propertyMap.MemberType, elementDiscriminator);
                    var serializer = propertyMap.GetSerializerForActualType(actualElementType);
                    object value = serializer.DeserializeElement(bsonReader, propertyMap.MemberType, out elementName);
                    propertyMap.Setter(obj, value);
                    missingElementPropertyMaps.Remove(propertyMap);
                } else {
                    // TODO: send extra elements to a catch-all property
                    if (classMap.IgnoreExtraElements) {
                        bsonReader.SkipElement();
                    } else {
                        string message = string.Format("Unexpected element: {0}", elementName);
                        throw new FileFormatException(message);
                    }
                }
            }
            bsonReader.ReadEndDocument();

            foreach (var propertyMap in missingElementPropertyMaps) {
                if (propertyMap.IsRequired) {
                    var message = string.Format("Required element is missing: {0}", propertyMap.ElementName);
                    throw new BsonSerializationException(message);
                }

                if (propertyMap.HasDefaultValue) {
                    propertyMap.ApplyDefaultValue(obj);
                }
            }

            return obj;
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            VerifyNominalType(nominalType);
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else {
                bsonReader.ReadDocumentName(out name);
                return DeserializeDocument(bsonReader, nominalType);
            }
        }

        public bool DocumentHasIdProperty(
            object document
        ) {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            return classMap.IdMemberMap != null;
        }

        public bool DocumentHasIdValue(
            object document,
            out object existingId
        ) {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            var idPropertyMap = classMap.IdMemberMap;
            existingId = idPropertyMap.Getter(document);
            return !idPropertyMap.IdGenerator.IsEmpty(existingId);
        }

        public void GenerateDocumentId(
            object document
        ) {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            var idPropertyMap = classMap.IdMemberMap;
            idPropertyMap.Setter(document, idPropertyMap.IdGenerator.GenerateId());
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object obj,
            bool serializeIdFirst
        ) {
            VerifyNominalType(nominalType);
            var actualType = obj.GetType();
            var classMap = BsonClassMap.LookupClassMap(actualType);

            bsonWriter.WriteStartDocument();
            BsonMemberMap idPropertyMap = null;
            if (serializeIdFirst) {
                idPropertyMap = classMap.IdMemberMap;
                if (idPropertyMap != null) {
                    SerializeProperty(bsonWriter, obj, idPropertyMap);
                }
            }

            if (classMap.DiscriminatorIsRequired || actualType != nominalType) {
                bsonWriter.WriteString("_t", classMap.Discriminator);
            }

            foreach (var propertyMap in classMap.MemberMaps) {
                // note: if serializeIdFirst is false then idPropertyMap will be null (so no property will be skipped)
                if (propertyMap != idPropertyMap) {
                    SerializeProperty(bsonWriter, obj, propertyMap);
                }
            }
            bsonWriter.WriteEndDocument();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            VerifyNominalType(nominalType);
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteDocumentName(name);
                SerializeDocument(bsonWriter, nominalType, value, false);
            }
        }
        #endregion

        #region private methods
        private string PeekDocumentDiscriminator(
            BsonReader bsonReader
        ) {
            bsonReader.PushBookmark();
            var discriminator = bsonReader.FindString("_t");
            bsonReader.PopBookmark();
            return discriminator;
        }

        private string PeekElementDiscriminator(
            BsonReader bsonReader,
            BsonType bsonType,
            string elementName
        ) {
            if (bsonType == BsonType.Document) {
                bsonReader.PushBookmark();
                bsonReader.ReadDocumentName(elementName);
                bsonReader.ReadStartDocument();
                var discriminator = bsonReader.FindString("_t");
                bsonReader.PopBookmark();
                return discriminator;
            } else {
                return null;
            }
        }

        private void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonMemberMap propertyMap
        ) {
            var value = propertyMap.Getter(obj);
            if (value == null && propertyMap.IgnoreIfNull) {
                return; // don't serialize null value
            }
            if (propertyMap.HasDefaultValue && !propertyMap.SerializeDefaultValue && value.Equals(propertyMap.DefaultValue)) {
                return; // don't serialize default value
            }

            var nominalType = propertyMap.MemberType;
            var actualType = (value == null) ? nominalType : value.GetType();
            var serializer = propertyMap.GetSerializerForActualType(actualType);
            var elementName = propertyMap.ElementName;
            var useCompactRepresentation = propertyMap.UseCompactRepresentation;
            serializer.SerializeElement(bsonWriter, nominalType, elementName, value, useCompactRepresentation);
        }

        private void VerifyNominalType(
            Type nominalType
        ) {
            if (
                !(nominalType.IsClass || nominalType.IsInterface) ||
                typeof(Array).IsAssignableFrom(nominalType)
            ) {
                string message = string.Format("BsonClassMapSerializer cannot be used with type: {0}", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
