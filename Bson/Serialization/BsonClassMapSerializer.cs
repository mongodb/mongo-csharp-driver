/* Copyright 2010-2012 10gen Inc.
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a serializer for a class map.
    /// </summary>
    public class BsonClassMapSerializer : IBsonSerializer
    {
        // private fields
        private BsonClassMap _classMap;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMapSerializer class.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        public BsonClassMapSerializer(BsonClassMap classMap)
        {
            _classMap = classMap;
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            VerifyNominalType(nominalType);
            if (bsonReader.GetCurrentBsonType() == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                var actualType = discriminatorConvention.GetActualType(bsonReader, nominalType);
                if (actualType != nominalType)
                {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    if (serializer != this)
                    {
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
            IBsonSerializationOptions options)
        {
            VerifyNominalType(nominalType);
            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                if (actualType.IsValueType)
                {
                    var message = string.Format("Value class {0} cannot be deserialized.", actualType.FullName);
                    throw new BsonSerializationException(message);
                }

                var classMap = BsonClassMap.LookupClassMap(actualType);
                if (classMap.IsAnonymous)
                {
                    throw new InvalidOperationException("An anonymous class cannot be deserialized.");
                }
                var obj = classMap.CreateInstance();

                if (bsonType != BsonType.Document)
                {
                    var message = string.Format(
                        "Expected a nested document representing the serialized form of a {0} value, but found a value of type {1} instead.",
                        actualType.FullName, bsonType);
                    throw new FileFormatException(message);
                }

                var supportsInitialization = obj as ISupportInitialize;
                if (supportsInitialization != null)
                {
                    supportsInitialization.BeginInit();
                }

                bsonReader.ReadStartDocument();
                var missingElementMemberMaps = new HashSet<BsonMemberMap>(classMap.AllMemberMaps); // make a copy!
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var elementName = bsonReader.ReadName();
                    if (elementName == discriminatorConvention.ElementName)
                    {
                        bsonReader.SkipValue(); // skip over discriminator
                        continue;
                    }

                    var memberMap = classMap.GetMemberMapForElement(elementName);
                    if (memberMap != null && memberMap != classMap.ExtraElementsMemberMap)
                    {
                        DeserializeMember(bsonReader, obj, memberMap);
                        missingElementMemberMaps.Remove(memberMap);
                    }
                    else
                    {
                        if (classMap.ExtraElementsMemberMap != null)
                        {
                            DeserializeExtraElement(bsonReader, obj, elementName, classMap.ExtraElementsMemberMap);
                            missingElementMemberMaps.Remove(classMap.ExtraElementsMemberMap);
                        }
                        else if (classMap.IgnoreExtraElements)
                        {
                            bsonReader.SkipValue();
                        }
                        else
                        {
                            var message = string.Format(
                                "Element '{0}' does not match any field or property of class {1}.",
                                elementName, classMap.ClassType.FullName);
                            throw new FileFormatException(message);
                        }
                    }
                }
                bsonReader.ReadEndDocument();

                foreach (var memberMap in missingElementMemberMaps)
                {
                    if (memberMap.IsRequired)
                    {
                        var fieldOrProperty = (memberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property";
                        var message = string.Format(
                            "Required element '{0}' for {1} '{2}' of class {3} is missing.",
                            memberMap.ElementName, fieldOrProperty, memberMap.MemberName, classMap.ClassType.FullName);
                        throw new FileFormatException(message);
                    }

                    memberMap.ApplyDefaultValue(obj);
                }

                if (supportsInitialization != null)
                {
                    supportsInitialization.EndInit();
                }

                return obj;
            }
        }

        /// <summary>
        /// Get the default serialization options for this serializer.
        /// </summary>
        /// <returns>The default serialization options for this serializer.</returns>
        public IBsonSerializationOptions GetDefaultSerializationOptions()
        {
            return null;
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
            out IIdGenerator idGenerator)
        {
            var classMap = BsonClassMap.LookupClassMap(document.GetType());
            var idMemberMap = classMap.IdMemberMap;
            if (idMemberMap != null)
            {
                id = idMemberMap.Getter(document);
                idNominalType = idMemberMap.MemberType;
                idGenerator = idMemberMap.IdGenerator;
                return true;
            }
            else
            {
                id = null;
                idNominalType = null;
                idGenerator = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        public BsonSerializationInfo GetItemSerializationInfo()
        {
            throw new NotSupportedException("BsonClassMapSerializer does not implement the GetItemSerializationInfo method.");
        }

        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>The serialization info for the member.</returns>
        public BsonSerializationInfo GetMemberSerializationInfo(string memberName)
        {
            foreach (var memberMap in _classMap.AllMemberMaps)
            {
                if (memberMap.MemberName == memberName)
                {
                    var elementName = memberMap.ElementName;
                    var serializer = memberMap.GetSerializer(memberMap.MemberType);
                    var nominalType = memberMap.MemberType;
                    var serializationOptions = memberMap.SerializationOptions;
                    return new BsonSerializationInfo(elementName, serializer, nominalType, serializationOptions);
                }
            }

            var message = string.Format(
                "Class {0} does not have a member called {1}.",
                BsonUtils.GetFriendlyTypeName(_classMap.ClassType),
                memberName);
            throw new ArgumentOutOfRangeException("memberName", message);
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
            IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                // Nullable types are weird because they get boxed as their underlying value type
                // we can best handle that by switching the nominalType to the underlying value type
                // (so VerifyNominalType doesn't fail and we don't get an unnecessary discriminator)
                if (nominalType.IsGenericType && nominalType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    nominalType = nominalType.GetGenericArguments()[0];
                }

                VerifyNominalType(nominalType);
                var actualType = (value == null) ? nominalType : value.GetType();
                var classMap = BsonClassMap.LookupClassMap(actualType);

                var documentSerializationOptions = (options ?? DocumentSerializationOptions.Defaults) as DocumentSerializationOptions;
                if (documentSerializationOptions == null)
                {
                    var message = string.Format(
                        "Serializer BsonClassMapSerializer expected serialization options of type {0}, not {1}.",
                        BsonUtils.GetFriendlyTypeName(typeof(DocumentSerializationOptions)),
                        BsonUtils.GetFriendlyTypeName(options.GetType()));
                    throw new BsonSerializationException(message);
                }

                bsonWriter.WriteStartDocument();
                BsonMemberMap idMemberMap = null;
                if (documentSerializationOptions.SerializeIdFirst)
                {
                    idMemberMap = classMap.IdMemberMap;
                    if (idMemberMap != null)
                    {
                        SerializeMember(bsonWriter, value, idMemberMap);
                    }
                }

                if (actualType != nominalType || classMap.DiscriminatorIsRequired || classMap.HasRootClass)
                {
                    // never write out a discriminator for an anonymous class
                    if (!classMap.IsAnonymous)
                    {
                        var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                        var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
                        if (discriminator != null)
                        {
                            bsonWriter.WriteName(discriminatorConvention.ElementName);
                            discriminator.WriteTo(bsonWriter);
                        }
                    }
                }

                foreach (var memberMap in classMap.AllMemberMaps)
                {
                    // note: if serializeIdFirst is false then idMemberMap will be null (so no property will be skipped)
                    if (memberMap != idMemberMap)
                    {
                        if (memberMap == classMap.ExtraElementsMemberMap)
                        {
                            SerializeExtraElements(bsonWriter, value, memberMap);
                        }
                        else
                        {
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
        public void SetDocumentId(object document, object id)
        {
            var documentType = document.GetType();
            if (documentType.IsValueType)
            {
                var message = string.Format("SetDocumentId cannot be used with value type {0}.", documentType.FullName);
                throw new BsonSerializationException(message);
            }

            var classMap = BsonClassMap.LookupClassMap(documentType);
            var idMemberMap = classMap.IdMemberMap;
            if (idMemberMap != null)
            {
                idMemberMap.Setter(document, id);
            }
            else
            {
                var message = string.Format("Class {0} has no Id member.", document.GetType().FullName);
                throw new InvalidOperationException(message);
            }
        }

        // private methods
        private void DeserializeExtraElement(
            BsonReader bsonReader,
            object obj,
            string elementName,
            BsonMemberMap extraElementsMemberMap)
        {
            if (extraElementsMemberMap.MemberType == typeof(BsonDocument))
            {
                var extraElements = (BsonDocument)extraElementsMemberMap.Getter(obj);
                if (extraElements == null)
                {
                    extraElements = new BsonDocument();
                    extraElementsMemberMap.Setter(obj, extraElements);
                }
                var bsonValue = BsonValue.ReadFrom(bsonReader);
                extraElements[elementName] = bsonValue;
            }
            else
            {
                var extraElements = (IDictionary<string, object>)extraElementsMemberMap.Getter(obj);
                if (extraElements == null)
                {
                    if (extraElementsMemberMap.MemberType == typeof(IDictionary<string, object>))
                    {
                        extraElements = new Dictionary<string, object>();
                    }
                    else
                    {
                        extraElements = (IDictionary<string, object>)Activator.CreateInstance(extraElementsMemberMap.MemberType);
                    }
                    extraElementsMemberMap.Setter(obj, extraElements);
                }
                var bsonValue = BsonValue.ReadFrom(bsonReader);
                extraElements[elementName] = BsonTypeMapper.MapToDotNetValue(bsonValue);
            }
        }

        private void DeserializeMember(BsonReader bsonReader, object obj, BsonMemberMap memberMap)
        {
            try
            {
                var nominalType = memberMap.MemberType;
                Type actualType;
                if (bsonReader.GetCurrentBsonType() == BsonType.Null)
                {
                    actualType = nominalType;
                }
                else
                {
                    var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                    actualType = discriminatorConvention.GetActualType(bsonReader, nominalType); // returns nominalType if no discriminator found
                }
                var serializer = memberMap.GetSerializer(actualType);
                var value = serializer.Deserialize(bsonReader, nominalType, actualType, memberMap.SerializationOptions);
                memberMap.Setter(obj, value);
            }
            catch (Exception ex)
            {
                var message = string.Format(
                    "An error occurred while deserializing the {0} {1} of class {2}: {3}", // terminating period provided by nested message
                    memberMap.MemberName, (memberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property", obj.GetType().FullName, ex.Message);
                throw new FileFormatException(message, ex);
            }
        }

        private void SerializeExtraElements(BsonWriter bsonWriter, object obj, BsonMemberMap extraElementsMemberMap)
        {
            var extraElements = extraElementsMemberMap.Getter(obj);
            if (extraElements != null)
            {
                if (extraElementsMemberMap.MemberType == typeof(BsonDocument))
                {
                    var bsonDocument = (BsonDocument)extraElements;
                    foreach (var element in bsonDocument)
                    {
                        element.WriteTo(bsonWriter);
                    }
                }
                else
                {
                    var dictionary = (IDictionary<string, object>)extraElements;
                    foreach (var key in dictionary.Keys)
                    {
                        bsonWriter.WriteName(key);
                        var value = dictionary[key];
                        if (value == null)
                        {
                            bsonWriter.WriteNull();
                        }
                        else
                        {
                            var bsonValue = BsonTypeMapper.MapToBsonValue(dictionary[key]);
                            bsonValue.WriteTo(bsonWriter);
                        }
                    }
                }
            }
        }

        private void SerializeMember(BsonWriter bsonWriter, object obj, BsonMemberMap memberMap)
        {
            var value = memberMap.Getter(obj);

            if (!memberMap.ShouldSerialize(obj, value))
            {
                return; // don't serialize member
            }

            bsonWriter.WriteName(memberMap.ElementName);
            var nominalType = memberMap.MemberType;
            var actualType = (value == null) ? nominalType : value.GetType();
            var serializer = memberMap.GetSerializer(actualType);
            serializer.Serialize(bsonWriter, nominalType, value, memberMap.SerializationOptions);
        }

        private void VerifyNominalType(Type nominalType)
        {
            if (!(nominalType.IsClass || (nominalType.IsValueType && !nominalType.IsPrimitive) || nominalType.IsInterface) ||
                typeof(Array).IsAssignableFrom(nominalType))
            {
                string message = string.Format("BsonClassMapSerializer cannot be used with type {0}.", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
    }
}
