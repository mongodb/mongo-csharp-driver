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
            var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
            var actualType = discriminatorConvention.GetActualDocumentType(bsonReader, nominalType);
            if (actualType != nominalType) {
                var serializer = BsonSerializer.LookupSerializer(actualType);
                if (serializer != this) {
                    // in rare cases a concrete actualType might have a more specialized serializer
                    return serializer.DeserializeDocument(bsonReader, actualType);
                }
            }

            var classMap = BsonClassMap.LookupClassMap(actualType);
            if (classMap.IsAnonymous) {
                throw new InvalidOperationException("Anonymous classes cannot be deserialized");
            }
            var obj = classMap.CreateInstance();

            var missingElementMemberMaps = new HashSet<BsonMemberMap>(classMap.MemberMaps); // make a copy!
            BsonType bsonType;
            string elementName;
            while (bsonReader.HasElement(out bsonType, out elementName)) {
                if (elementName == discriminatorConvention.ElementName) {
                    bsonReader.SkipElement(elementName); // skip over discriminator
                    continue;
                }

                var memberMap = classMap.GetMemberMapForElement(elementName);
                if (memberMap != null) {
                    var actualElementType = BsonDefaultSerializer.GetActualElementType(bsonReader, memberMap.MemberType); // returns nominalType if no discriminator found
                    var serializer = memberMap.GetSerializerForActualType(actualElementType);
                    object value = serializer.DeserializeElement(bsonReader, memberMap.MemberType, out elementName);
                    memberMap.Setter(obj, value);
                    missingElementMemberMaps.Remove(memberMap);
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

            foreach (var memberMap in missingElementMemberMaps) {
                if (memberMap.IsRequired) {
                    var message = string.Format("Required element is missing: {0}", memberMap.ElementName);
                    throw new BsonSerializationException(message);
                }

                if (memberMap.HasDefaultValue) {
                    memberMap.ApplyDefaultValue(obj);
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

        public bool DocumentHasIdMember(
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
            var idMemberMap = classMap.IdMemberMap;
            existingId = idMemberMap.Getter(document);
            return !idMemberMap.IdGenerator.IsEmpty(existingId);
        }

        public void GenerateDocumentId(
            object document
        ) {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            var idMemberMap = classMap.IdMemberMap;
            idMemberMap.Setter(document, idMemberMap.IdGenerator.GenerateId());
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object obj,
            bool serializeIdFirst
        ) {
            // Nullable types are weird because they get boxed as their underlying value type
            // we can best handle that by switching the nominalType to the underlying value type
            // (so VerifyNominalType doesn't fail and we don't get an unnecessary discriminator)
            if (nominalType.IsGenericType && nominalType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                nominalType = nominalType.GetGenericArguments()[0];
            }

            VerifyNominalType(nominalType);
            var actualType = obj.GetType();
            var classMap = BsonClassMap.LookupClassMap(actualType);

            bsonWriter.WriteStartDocument();
            BsonMemberMap idMemberMap = null;
            if (serializeIdFirst) {
                idMemberMap = classMap.IdMemberMap;
                if (idMemberMap != null) {
                    SerializeMember(bsonWriter, obj, idMemberMap);
                }
            }

            if (actualType != nominalType || classMap.DiscriminatorIsRequired || classMap.HasRootClass) {
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(actualType);
                var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
                if (discriminator != null) {
                    var element = new BsonElement(discriminatorConvention.ElementName, discriminator);
                    element.WriteTo(bsonWriter);
                }
            }

            foreach (var memberMap in classMap.MemberMaps) {
                // note: if serializeIdFirst is false then idMemberMap will be null (so no property will be skipped)
                if (memberMap != idMemberMap) {
                    SerializeMember(bsonWriter, obj, memberMap);
                }
            }
            bsonWriter.WriteEndDocument();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
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
        private void SerializeMember(
            BsonWriter bsonWriter,
            object obj,
            BsonMemberMap memberMap
        ) {
            var value = memberMap.Getter(obj);
            if (value == null && memberMap.IgnoreIfNull) {
                return; // don't serialize null value
            }
            if (memberMap.HasDefaultValue && !memberMap.SerializeDefaultValue && value.Equals(memberMap.DefaultValue)) {
                return; // don't serialize default value
            }

            var nominalType = memberMap.MemberType;
            var actualType = (value == null) ? nominalType : value.GetType();
            var serializer = memberMap.GetSerializerForActualType(actualType);
            var elementName = memberMap.ElementName;
            serializer.SerializeElement(bsonWriter, nominalType, elementName, value);
        }

        private void VerifyNominalType(
            Type nominalType
        ) {
            if (
                !(nominalType.IsClass || (nominalType.IsValueType && !nominalType.IsPrimitive) || nominalType.IsInterface) ||
                typeof(Array).IsAssignableFrom(nominalType)
            ) {
                string message = string.Format("BsonClassMapSerializer cannot be used with type: {0}", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
