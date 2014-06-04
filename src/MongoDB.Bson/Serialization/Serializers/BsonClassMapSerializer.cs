/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a serializer for a class map.
    /// </summary>
    public class BsonClassMapSerializer<TClass> : SerializerBase<TClass>, IBsonIdProvider, IBsonDocumentSerializer, IBsonPolymorphicSerializer
    {
        // private fields
        private BsonClassMap<TClass> _classMap;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMapSerializer class.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        public BsonClassMapSerializer(BsonClassMap<TClass> classMap)
        {
            if (classMap == null)
            {
                throw new ArgumentNullException("classMap");
            }

            _classMap = classMap;
        }

        // public properties
        /// <summary>
        /// Gets a value indicating whether this serializer's discriminator is compatible with the object serializer.
        /// </summary>
        /// <value>
        /// <c>true</c> if this serializer's discriminator is compatible with the object serializer; otherwise, <c>false</c>.
        /// </value>
        public bool IsDiscriminatorCompatibleWithObjectSerializer
        {
            get { return true; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override TClass Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            if (_classMap.ClassType.IsValueType)
            {
                var message = string.Format("Value class {0} cannot be deserialized.", _classMap.ClassType.FullName);
                throw new BsonSerializationException(message);
            }

            if (_classMap.IsAnonymous)
            {
                throw new InvalidOperationException("An anonymous class cannot be deserialized.");
            }

            if (bsonReader.GetCurrentBsonType() == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return default(TClass);
            }
            else
            {
                var discriminatorConvention = _classMap.GetDiscriminatorConvention();

                var actualType = discriminatorConvention.GetActualType(bsonReader, context.NominalType);
                if (actualType == typeof(TClass))
                {
                    return DeserializeClass(context);
                }
                else
                {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    return (TClass)serializer.Deserialize(context);
                }

            }
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public TClass DeserializeClass(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType != BsonType.Document)
            {
                var message = string.Format(
                    "Expected a nested document representing the serialized form of a {0} value, but found a value of type {1} instead.",
                    typeof(TClass).FullName, bsonType);
                throw new FileFormatException(message);
            }

            Dictionary<string, object> values = null;
            var document = default(TClass);
            ISupportInitialize supportsInitialization = null;
            if (_classMap.HasCreatorMaps)
            {
                // for creator-based deserialization we first gather the values in a dictionary and then call a matching creator
                values = new Dictionary<string, object>();
            }
            else
            {
                // for mutable classes we deserialize the values directly into the result object
                document = _classMap.CreateInstance();

                supportsInitialization = document as ISupportInitialize;
                if (supportsInitialization != null)
                {
                    supportsInitialization.BeginInit();
                }
            }

            var discriminatorConvention = _classMap.GetDiscriminatorConvention();
            var allMemberMaps = _classMap.AllMemberMaps;
            var extraElementsMemberMapIndex = _classMap.ExtraElementsMemberMapIndex;
            var memberMapBitArray = FastMemberMapHelper.GetBitArray(allMemberMaps.Count);

            bsonReader.ReadStartDocument();
            var elementTrie = _classMap.ElementTrie;
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var trieDecoder = new TrieNameDecoder<int>(elementTrie);
                var elementName = bsonReader.ReadName(trieDecoder);

                if (trieDecoder.Found)
                {
                    var memberMapIndex = trieDecoder.Value;
                    var memberMap = allMemberMaps[memberMapIndex];
                    if (memberMapIndex != extraElementsMemberMapIndex)
                    {
                        if (document != null)
                        {
                            if (memberMap.IsReadOnly)
                            {
                                bsonReader.SkipValue();
                            }
                            else
                            {
                                var value = DeserializeMemberValue(context, memberMap);
                                memberMap.Setter(document, value);
                            }
                        }
                        else
                        {
                            var value = DeserializeMemberValue(context, memberMap);
                            values[elementName] = value;
                        }
                    }
                    else
                    {
                        DeserializeExtraElement(context, document, elementName, memberMap);
                    }
                    memberMapBitArray[memberMapIndex >> 5] |= 1U << (memberMapIndex & 31);
                }
                else
                {
                    if (elementName == discriminatorConvention.ElementName)
                    {
                        bsonReader.SkipValue(); // skip over discriminator
                        continue;
                    }

                    if (extraElementsMemberMapIndex >= 0)
                    {
                        DeserializeExtraElement(context, document, elementName, _classMap.ExtraElementsMemberMap);
                        memberMapBitArray[extraElementsMemberMapIndex >> 5] |= 1U << (extraElementsMemberMapIndex & 31);
                    }
                    else if (_classMap.IgnoreExtraElements)
                    {
                        bsonReader.SkipValue();
                    }
                    else
                    {
                        var message = string.Format(
                            "Element '{0}' does not match any field or property of class {1}.",
                            elementName, _classMap.ClassType.FullName);
                        throw new FileFormatException(message);
                    }
                }
            }
            bsonReader.ReadEndDocument();

            // check any members left over that we didn't have elements for (in blocks of 32 elements at a time)
            for (var bitArrayIndex = 0; bitArrayIndex < memberMapBitArray.Length; ++bitArrayIndex)
            {
                var memberMapIndex = bitArrayIndex << 5;
                var memberMapBlock = ~memberMapBitArray[bitArrayIndex]; // notice that bits are flipped so 1's are now the missing elements

                // work through this memberMapBlock of 32 elements
                while (true)
                {
                    // examine missing elements (memberMapBlock is shifted right as we work through the block)
                    for (; (memberMapBlock & 1) != 0; ++memberMapIndex, memberMapBlock >>= 1)
                    {
                        var memberMap = allMemberMaps[memberMapIndex];
                        if (memberMap.IsReadOnly)
                        {
                            continue;
                        }

                        if (memberMap.IsRequired)
                        {
                            var fieldOrProperty = (memberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property";
                            var message = string.Format(
                                "Required element '{0}' for {1} '{2}' of class {3} is missing.",
                                memberMap.ElementName, fieldOrProperty, memberMap.MemberName, _classMap.ClassType.FullName);
                            throw new FileFormatException(message);
                        }

                        if (document != null)
                        {
                            memberMap.ApplyDefaultValue(document);
                        }
                        else if (memberMap.IsDefaultValueSpecified && !memberMap.IsReadOnly)
                        {
                            values[memberMap.ElementName] = memberMap.DefaultValue;
                        }
                    }

                    if (memberMapBlock == 0)
                    {
                        break;
                    }

                    // skip ahead to the next missing element
                    var leastSignificantBit = FastMemberMapHelper.GetLeastSignificantBit(memberMapBlock);
                    memberMapIndex += leastSignificantBit;
                    memberMapBlock >>= leastSignificantBit;
                }
            }

            if (document != null)
            {
                if (supportsInitialization != null)
                {
                    supportsInitialization.EndInit();
                }

                return document;
            }
            else
            {
                return CreateInstanceUsingCreator(values);
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
            out IIdGenerator idGenerator)
        {
            var idMemberMap = _classMap.IdMemberMap;
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
                    var serializer = memberMap.GetSerializer();
                    var nominalType = memberMap.MemberType;
                    return new BsonSerializationInfo(elementName, serializer, serializer.ValueType);
                }
            }

            var message = string.Format(
                "Class {0} does not have a member called {1}.",
                BsonUtils.GetFriendlyTypeName(_classMap.ClassType),
                memberName);
            throw new ArgumentOutOfRangeException("memberName", message);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, TClass value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                if (actualType == typeof(TClass))
                {
                    SerializeClass(context, value);
                }
                else
                {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    serializer.Serialize(context, value);
                }
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

            var idMemberMap = _classMap.IdMemberMap;
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
        private BsonCreatorMap ChooseBestCreator(Dictionary<string, object> values)
        {
            // there's only one selector for now, but there might be more in the future (possibly even user provided)
            var selector = new MostArgumentsCreatorSelector();
            var creatorMap = selector.SelectCreator(_classMap, values);

            if (creatorMap == null)
            {
                throw new BsonSerializationException("No matching creator found.");
            }

            return creatorMap;
        }

        private TClass CreateInstanceUsingCreator(Dictionary<string, object> values)
        {
            var creatorMap = ChooseBestCreator(values);
            var document = creatorMap.CreateInstance(values); // removes values consumed

            var supportsInitialization = document as ISupportInitialize;
            if (supportsInitialization != null)
            {
                supportsInitialization.BeginInit();
            }

            // process any left over values that weren't passed to the creator
            foreach (var keyValuePair in values)
            {
                var elementName = keyValuePair.Key;
                var value = keyValuePair.Value;

                var memberMap = _classMap.GetMemberMapForElement(elementName);
                if (!memberMap.IsReadOnly)
                {
                    memberMap.Setter.Invoke(document, value);
                }
            }

            if (supportsInitialization != null)
            {
                supportsInitialization.EndInit();
            }

            return (TClass)document;
        }

        private void DeserializeExtraElement(
            BsonDeserializationContext context,
            object obj,
            string elementName,
            BsonMemberMap extraElementsMemberMap)
        {
            var bsonReader = context.Reader;

            if (extraElementsMemberMap.MemberType == typeof(BsonDocument))
            {
                var extraElements = (BsonDocument)extraElementsMemberMap.Getter(obj);
                if (extraElements == null)
                {
                    extraElements = new BsonDocument();
                    extraElementsMemberMap.Setter(obj, extraElements);
                }
                var bsonValue = context.DeserializeWithChildContext(BsonValueSerializer.Instance);
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
                var bsonValue = context.DeserializeWithChildContext(BsonValueSerializer.Instance);
                extraElements[elementName] = BsonTypeMapper.MapToDotNetValue(bsonValue);
            }
        }

        private object DeserializeMemberValue(BsonDeserializationContext context, BsonMemberMap memberMap)
        {
            var bsonReader = context.Reader;

            try
            {
                return context.DeserializeWithChildContext(memberMap.GetSerializer());
            }
            catch (Exception ex)
            {
                var message = string.Format(
                    "An error occurred while deserializing the {0} {1} of class {2}: {3}", // terminating period provided by nested message
                    memberMap.MemberName, (memberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property", memberMap.ClassMap.ClassType.FullName, ex.Message);
                throw new FileFormatException(message, ex);
            }
        }

        private void SerializeClass(BsonSerializationContext context, TClass document)
        {
            var bsonWriter = context.Writer;

            var remainingMemberMaps = _classMap.AllMemberMaps.ToList();

            bsonWriter.WriteStartDocument();

            var idMemberMap = _classMap.IdMemberMap;
            if (idMemberMap != null && context.SerializeIdFirst)
            {
                SerializeMember(context, document, idMemberMap);
                remainingMemberMaps.Remove(idMemberMap);
            }

            //var autoTimeStampMemberMap = _classMap.AutoTimeStampMemberMap;
            //if (autoTimeStampMemberMap != null)
            //{
            //    SerializeNormalMember(context, document, autoTimeStampMemberMap);
            //    remainingMemberMaps.Remove(autoTimeStampMemberMap);
            //}

            if (ShouldSerializeDiscriminator(context))
            {
                SerializeDiscriminator(context, document);
            }

            foreach (var memberMap in remainingMemberMaps)
            {
                SerializeMember(context, document, memberMap);
            }

            bsonWriter.WriteEndDocument();
        }

        private void SerializeExtraElements(BsonSerializationContext context, object obj, BsonMemberMap extraElementsMemberMap)
        {
            var bsonWriter = context.Writer;

            var extraElements = extraElementsMemberMap.Getter(obj);
            if (extraElements != null)
            {
                if (extraElementsMemberMap.MemberType == typeof(BsonDocument))
                {
                    var bsonDocument = (BsonDocument)extraElements;
                    foreach (var element in bsonDocument)
                    {
                        bsonWriter.WriteName(element.Name);
                        context.SerializeWithChildContext(BsonValueSerializer.Instance, element.Value);
                    }
                }
                else
                {
                    var dictionary = (IDictionary<string, object>)extraElements;
                    foreach (var key in dictionary.Keys)
                    {
                        bsonWriter.WriteName(key);
                        var value = dictionary[key];
                        var bsonValue = BsonTypeMapper.MapToBsonValue(value);
                        context.SerializeWithChildContext(BsonValueSerializer.Instance, bsonValue);
                    }
                }
            }
        }

        private void SerializeDiscriminator(BsonSerializationContext context, object obj)
        {
            var discriminatorConvention = _classMap.GetDiscriminatorConvention();
            if (discriminatorConvention != null)
            {
                var actualType = obj.GetType();
                var discriminator = discriminatorConvention.GetDiscriminator(context.NominalType, actualType);
                if (discriminator != null)
                {
                    context.Writer.WriteName(discriminatorConvention.ElementName);
                    context.SerializeWithChildContext(BsonValueSerializer.Instance, discriminator);
                }
            }
        }

        private void SerializeMember(BsonSerializationContext context, object obj, BsonMemberMap memberMap)
        {
            if (memberMap != _classMap.ExtraElementsMemberMap)
            {
                SerializeNormalMember(context, obj, memberMap);
            }
            else
            {
                SerializeExtraElements(context, obj, memberMap);
            }
        }

        private void SerializeNormalMember(BsonSerializationContext context, object obj, BsonMemberMap memberMap)
        {
            var bsonWriter = context.Writer;

            var value = memberMap.Getter(obj);

            if (!memberMap.ShouldSerialize(obj, value))
            {
                return; // don't serialize member
            }

            bsonWriter.WriteName(memberMap.ElementName);
            context.SerializeWithChildContext(memberMap.GetSerializer(), value);
        }

        private bool ShouldSerializeDiscriminator(BsonSerializationContext context)
        {
            return (context.NominalType != _classMap.ClassType || _classMap.DiscriminatorIsRequired || _classMap.HasRootClass) && !_classMap.IsAnonymous;
        }

        // nested classes
        // helper class that implements member map bit array helper functions
        private static class FastMemberMapHelper
        {
            public static uint[] GetBitArray(int memberCount)
            {
                var bitArrayOffset = memberCount & 31;
                var bitArrayLength = memberCount >> 5;
                if (bitArrayOffset == 0)
                {
                    return new uint[bitArrayLength];
                }
                var bitArray = new uint[bitArrayLength + 1];
                bitArray[bitArrayLength] = ~0U << bitArrayOffset; // set unused bits to 1
                return bitArray;
            }

            // see http://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightBinSearch
            // also returns 31 if no bits are set; caller must check this case
            public static int GetLeastSignificantBit(uint bitBlock)
            {
                var leastSignificantBit = 1;
                if ((bitBlock & 65535) == 0)
                {
                    bitBlock >>= 16;
                    leastSignificantBit |= 16;
                }
                if ((bitBlock & 255) == 0)
                {
                    bitBlock >>= 8;
                    leastSignificantBit |= 8;
                }
                if ((bitBlock & 15) == 0)
                {
                    bitBlock >>= 4;
                    leastSignificantBit |= 4;
                }
                if ((bitBlock & 3) == 0)
                {
                    bitBlock >>= 2;
                    leastSignificantBit |= 2;
                }
                return leastSignificantBit - (int)(bitBlock & 1);
            }
        }
    }
}
