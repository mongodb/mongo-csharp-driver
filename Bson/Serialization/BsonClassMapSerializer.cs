/* Copyright 2010-2011 10gen Inc.
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
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization {
    /// <summary>
    /// Represents a serializer for a class map.
    /// </summary>
    public class BsonClassMapSerializer : IBsonSerializer {
        #region private static fields
        private static BsonClassMapSerializer instance = new BsonClassMapSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMapSerializer class.
        /// </summary>
        public BsonClassMapSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonClassMapSerializer class.
        /// </summary>
        public static BsonClassMapSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
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

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
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
                if (actualType.IsValueType) {
                    var message = string.Format("Value class {0} cannot be deserialized.", actualType.FullName);
                    throw new BsonSerializationException(message);
                }

                var classMap = BsonClassMap.LookupClassMap(actualType);
                if (classMap.IsAnonymous) {
                    throw new InvalidOperationException("An anonymous class cannot be deserialized.");
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
                    if (memberMap != null && memberMap != classMap.ExtraElementsMemberMap) {
                        DeserializeMember(bsonReader, obj, memberMap);
                        missingElementMemberMaps.Remove(memberMap);
                    } else {
                        if (classMap.ExtraElementsMemberMap != null) {
                            DeserializeExtraElement(bsonReader, obj, elementName, classMap.ExtraElementsMemberMap);
                        } else if (classMap.IgnoreExtraElements) {
                            bsonReader.SkipValue();
                        } else {
                            var message = string.Format("Element '{0}' does not match any field or property of class {1}.", elementName, classMap.ClassType.FullName);
                            throw new FileFormatException(message);
                        }
                    }
                }
                bsonReader.ReadEndDocument();

                foreach (var memberMap in missingElementMemberMaps) {
                    if (memberMap.IsRequired) {
                        var fieldOrProperty = (memberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property";
                        var message = string.Format("Required element '{0}' for {1} '{2}' of class {3} is missing.", memberMap.ElementName, fieldOrProperty, memberMap.MemberName, classMap.ClassType.FullName);
                        throw new FileFormatException(message);
                    }

                    if (memberMap.HasDefaultValue) {
                        memberMap.ApplyDefaultValue(obj);
                    }
                }

                return obj;
            }
        }

        /// <summary>
        /// Gets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        /// <param name="idNominalType">The nominal type of the Id.</param>
        /// <param name="idGenerator">The IdGenerator for the Id type.</param>
        /// <returns>True if the document has an Id.</returns>
        public bool GetDocumentId(
            object document,
            out object id,
            out Type idNominalType,
            out IIdGenerator idGenerator
        ) {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            var idMemberMap = classMap.IdMemberMap;
            if (idMemberMap != null) {
                id = idMemberMap.Getter(document);
                idNominalType = idMemberMap.MemberType;
                idGenerator = idMemberMap.IdGenerator;
                return true;
            } else {
                id = null;
                idNominalType = null;
                idGenerator = null;
                return false;
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
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
                        if (memberMap == classMap.ExtraElementsMemberMap) {
                            SerializeExtraElements(bsonWriter, value, memberMap);
                        } else {
                            SerializeMember(bsonWriter, value, memberMap);
                        }
                    }
                }
                bsonWriter.WriteEndDocument();
            }
        }

        /// <summary>
        /// Sets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        public void SetDocumentId(
            object document,
            object id
        ) {
            var documentType = document.GetType();
            if (documentType.IsValueType) {
                var message = string.Format("SetDocumentId cannot be used with value type {0}.", documentType.FullName);
                throw new BsonSerializationException(message);
            }

            var classMap = BsonClassMap.LookupClassMap(documentType);
            var idMemberMap = classMap.IdMemberMap;
            if (idMemberMap != null) {
                idMemberMap.Setter(document, id);
            } else {
                var message = string.Format("Class {0} has no Id member.", document.GetType().FullName);
                throw new InvalidOperationException(message);
            }
        }
        #endregion

        #region private methods
        private void DeserializeExtraElement(
            BsonReader bsonReader,
            object obj,
            string elementName,
            BsonMemberMap extraElementsMemberMap
        ) {
            var extraElements = (BsonDocument) extraElementsMemberMap.Getter(obj);
            if (extraElements == null) {
                extraElements = new BsonDocument();
                extraElementsMemberMap.Setter(obj, extraElements);
            }
            var value = BsonValue.ReadFrom(bsonReader);
            extraElements[elementName] = value;
        }

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
            var serializer = memberMap.GetSerializer(actualType);
            var value = serializer.Deserialize(bsonReader, nominalType, actualType, memberMap.SerializationOptions);
            memberMap.Setter(obj, value);
        }

        private void SerializeExtraElements(
            BsonWriter bsonWriter,
            object obj,
            BsonMemberMap extraElementsMemberMap
        ) {
            var extraElements = (BsonDocument) extraElementsMemberMap.Getter(obj);
            if (extraElements != null) {
                foreach (var element in extraElements) {
                    element.WriteTo(bsonWriter);
                }
            }
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
            if (!memberMap.ShouldSerializeMethod(obj)) {
                return; // the ShouldSerializeMethod determined that the member shouldn't be serialized
            }

            bsonWriter.WriteName(memberMap.ElementName);
            var nominalType = memberMap.MemberType;
            var actualType = (value == null) ? nominalType : value.GetType();
            var serializer = memberMap.GetSerializer(actualType);
            serializer.Serialize(bsonWriter, nominalType, value, memberMap.SerializationOptions);
        }

        private void VerifyNominalType(
            Type nominalType
        ) {
            if (
                !(nominalType.IsClass || (nominalType.IsValueType && !nominalType.IsPrimitive) || nominalType.IsInterface) ||
                typeof(Array).IsAssignableFrom(nominalType)
            ) {
                string message = string.Format("BsonClassMapSerializer cannot be used with type {0}.", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
