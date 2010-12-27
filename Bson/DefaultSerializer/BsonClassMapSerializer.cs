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
        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            VerifyNominalType(nominalType);
            if (bsonReader.CurrentBsonType == Bson.BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                var actualType = discriminatorConvention.GetActualType(bsonReader, nominalType);
                if (actualType != nominalType) {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    if (serializer != this) {
                        // in rare cases a concrete actualType might have a more specialized serializer
                        return serializer.Deserialize(bsonReader, nominalType, actualType, options);
                    }
                }

                return Deserialize(bsonReader, nominalType, actualType, options);
            }
        }

        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyNominalType(nominalType);
            if (bsonReader.CurrentBsonType == Bson.BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var classMap = BsonClassMap.LookupClassMap(actualType);
                if (classMap.IsAnonymous) {
                    throw new InvalidOperationException("Anonymous classes cannot be deserialized");
                }
                var obj = classMap.CreateInstance();

                bsonReader.ReadStartDocument();
                var missingElementMemberMaps = new HashSet<BsonMemberMap>(classMap.MemberMaps); // make a copy!
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var elementName = bsonReader.ReadName();
                    if (elementName == discriminatorConvention.ElementName) {
                        bsonReader.SkipValue(); // skip over discriminator
                        continue;
                    }

                    var memberMap = classMap.GetMemberMapForElement(elementName);
                    if (memberMap != null) {
                        DeserializeMember(bsonReader, obj, memberMap);
                        missingElementMemberMaps.Remove(memberMap);
                    } else {
                        // TODO: send extra elements to a catch-all property
                        if (classMap.IgnoreExtraElements) {
                            bsonReader.SkipValue();
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
        }

        public bool GetDocumentId(
            object document,
            out object id,
            out IIdGenerator idGenerator
        ) {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            var idMemberMap = classMap.IdMemberMap;
            if (idMemberMap != null) {
                id = idMemberMap.Getter(document);
                idGenerator = idMemberMap.IdGenerator;
                return true;
            } else {
                id = null;
                idGenerator = null;
                return false;
            }
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
                // Nullable types are weird because they get boxed as their underlying value type
                // we can best handle that by switching the nominalType to the underlying value type
                // (so VerifyNominalType doesn't fail and we don't get an unnecessary discriminator)
                if (nominalType.IsGenericType && nominalType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                    nominalType = nominalType.GetGenericArguments()[0];
                }

                VerifyNominalType(nominalType);
                var actualType = (value == null) ? nominalType : value.GetType();
                var classMap = BsonClassMap.LookupClassMap(actualType);

                bsonWriter.WriteStartDocument();
                var documentOptions = (options == null) ? DocumentSerializationOptions.Defaults : (DocumentSerializationOptions) options;
                BsonMemberMap idMemberMap = null;
                if (documentOptions.SerializeIdFirst) {
                    idMemberMap = classMap.IdMemberMap;
                    if (idMemberMap != null) {
                        SerializeMember(bsonWriter, value, idMemberMap);
                    }
                }

                if (actualType != nominalType || classMap.DiscriminatorIsRequired || classMap.HasRootClass) {
                    // never write out a discriminator for an anonymous class
                    if (!classMap.IsAnonymous) {
                        var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                        var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
                        if (discriminator != null) {
                            bsonWriter.WriteName(discriminatorConvention.ElementName);
                            discriminator.WriteTo(bsonWriter);
                        }
                    }
                }

                foreach (var memberMap in classMap.MemberMaps) {
                    // note: if serializeIdFirst is false then idMemberMap will be null (so no property will be skipped)
                    if (memberMap != idMemberMap) {
                        SerializeMember(bsonWriter, value, memberMap);
                    }
                }
                bsonWriter.WriteEndDocument();
            }
        }

        public void SetDocumentId(
            object document,
            object id
        ) {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            var idMemberMap = classMap.IdMemberMap;
            if (idMemberMap != null) {
                idMemberMap.Setter(document, id);
            } else {
                var message = string.Format("Class {0} has no Id member", document.GetType());
                throw new InvalidOperationException(message);
            }
        }
        #endregion

        #region private methods
        private void DeserializeMember(
            BsonReader bsonReader,
            object obj,
            BsonMemberMap memberMap
        ) {
            var nominalType = memberMap.MemberType;
            Type actualType;
            if (bsonReader.CurrentBsonType == BsonType.Null) {
                actualType = nominalType;
            } else {
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                actualType = discriminatorConvention.GetActualType(bsonReader, nominalType); // returns nominalType if no discriminator found
            }
            var serializer = memberMap.GetSerializerForActualType(actualType);
            var value = serializer.Deserialize(bsonReader, nominalType, actualType, memberMap.SerializationOptions);
            memberMap.Setter(obj, value);
        }

        private void SerializeMember(
            BsonWriter bsonWriter,
            object obj,
            BsonMemberMap memberMap
        ) {
            var value = memberMap.Getter(obj);
            if (value == null && memberMap.IgnoreIfNull) {
                return; // don't serialize null value
            }
            if (memberMap.HasDefaultValue && !memberMap.SerializeDefaultValue && object.Equals(value, memberMap.DefaultValue)) {
                return; // don't serialize default value
            }

            var nominalType = memberMap.MemberType;
            var actualType = (value == null) ? nominalType : value.GetType();
            var serializer = memberMap.GetSerializerForActualType(actualType);
            var elementName = memberMap.ElementName;
            bsonWriter.WriteName(elementName);
            serializer.Serialize(bsonWriter, nominalType, value, memberMap.SerializationOptions);
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
