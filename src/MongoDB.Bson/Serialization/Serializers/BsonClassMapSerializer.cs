/* Copyright 2010-present MongoDB Inc.
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
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a serializer for a class map.
    /// </summary>
    /// <typeparam name="TClass">The type of the class.</typeparam>
    public sealed class BsonClassMapSerializer<TClass> : SerializerBase<TClass>, IBsonIdProvider, IBsonDocumentSerializer, IBsonPolymorphicSerializer, IHasDiscriminatorConvention, IHasSerializationDomain
    {
        // private fields
        private readonly BsonClassMap _classMap;
        private readonly IBsonSerializationDomain _serializationDomain;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMapSerializer class.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        public BsonClassMapSerializer(BsonClassMap classMap)
            : this(BsonSerializationDomain.Default, classMap)
        {
        }

        internal BsonClassMapSerializer(IBsonSerializationDomain serializationDomain, BsonClassMap classMap)
        {
            if (serializationDomain == null)
            {
                throw new ArgumentNullException(nameof(serializationDomain));
            }
            if (classMap == null)
            {
                throw new ArgumentNullException(nameof(classMap));
            }
            if (classMap.ClassType != typeof(TClass))
            {
                var message = string.Format("Must be a BsonClassMap for the type {0}.", typeof(TClass));
                throw new ArgumentException(message, nameof(classMap));
            }
            if (!classMap.IsFrozen)
            {
                throw new ArgumentException("Class map is not frozen.", nameof(classMap));
            }

            _serializationDomain = serializationDomain;
            _classMap = classMap;
        }

        // public properties
        /// <inheritdoc/>
        public IDiscriminatorConvention DiscriminatorConvention => _classMap.GetDiscriminatorConvention();

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

        IBsonSerializationDomain IHasSerializationDomain.SerializationDomain => _serializationDomain;

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override TClass Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return default(TClass);
            }

            var discriminatorConvention = _classMap.GetDiscriminatorConvention();

            var actualType = discriminatorConvention.GetActualType(bsonReader, args.NominalType);
            if (actualType == typeof(TClass))
            {
                return DeserializeClass(context);
            }

            var actualTypeSerializer = this.GetSerializerForDerivedType(actualType);
            return (TClass)actualTypeSerializer.Deserialize(context);
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>A deserialized value.</returns>
        public TClass DeserializeClass(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType != BsonType.Document)
            {
                var message = string.Format(
                    "Expected a nested document representing the serialized form of a {0} value, but found a value of type {1} instead.",
                    typeof(TClass).FullName, bsonType);
                throw new FormatException(message);
            }

            Dictionary<string, object> values = null;
            // Important for struct support:
            // should use object variable here, so created value should be boxed right away!
            object document = null;

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

                if (document == null)
                {
                    throw new BsonSerializationException($"{nameof(BsonClassMap)} did not provide an instance of {typeof(TClass).Name}.");
                }

                supportsInitialization = document as ISupportInitialize;
                if (supportsInitialization != null)
                {
                    supportsInitialization.BeginInit();
                }
            }

            var discriminatorConvention = _classMap.GetDiscriminatorConvention();
            var allMemberMaps = _classMap.AllMemberMaps;
            var extraElementsMemberMapIndex = _classMap.ExtraElementsMemberMapIndex;

            var (lengthInUInts, useStackAlloc) = FastMemberMapHelper.GetLengthInUInts(allMemberMaps.Count);
            using var bitArray = useStackAlloc ? FastMemberMapHelper.GetMembersBitArray(stackalloc uint[lengthInUInts]) : FastMemberMapHelper.GetMembersBitArray(lengthInUInts);

            bsonReader.ReadStartDocument();
            var elementTrie = _classMap.ElementTrie;
            var trieDecoder = new TrieNameDecoder<int>(elementTrie);
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
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
                        if (document != null)
                        {
                            DeserializeExtraElementMember(context, document, elementName, memberMap);
                        }
                        else
                        {
                            DeserializeExtraElementValue(context, values, elementName, memberMap);
                        }
                    }

                    bitArray.SetMemberIndex(memberMapIndex);
                }
                else
                {
                    if (elementName == discriminatorConvention.ElementName)
                    {
                        bsonReader.SkipValue(); // skip over discriminator
                        continue;
                    }

                    // In QE server returns __safeContent__ fields, which should be skipped over.
                    if (elementName == "__safeContent__")
                    {
                        bsonReader.SkipValue(); // skip over unwanted element
                        continue;
                    }

                    if (extraElementsMemberMapIndex >= 0)
                    {
                        var extraElementsMemberMap = _classMap.ExtraElementsMemberMap;
                        if (document != null)
                        {
                            DeserializeExtraElementMember(context, document, elementName, extraElementsMemberMap);
                        }
                        else
                        {
                            DeserializeExtraElementValue(context, values, elementName, extraElementsMemberMap);
                        }
                        bitArray.SetMemberIndex(extraElementsMemberMapIndex);
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
                        throw new FormatException(message);
                    }
                }
            }
            bsonReader.ReadEndDocument();

            // check any members left over that we didn't have elements for (in blocks of 32 elements at a time)
            var bitArraySpan = bitArray.Span;
            for (var bitArrayIndex = 0; bitArrayIndex < bitArraySpan.Length; bitArrayIndex++)
            {
                var memberMapIndex = bitArrayIndex << 5;
                var memberMapBlock = ~bitArraySpan[bitArrayIndex]; // notice that bits are flipped so 1's are now the missing elements

                // work through this memberMapBlock of 32 elements
                for (; memberMapBlock != 0 && memberMapIndex < allMemberMaps.Count; memberMapIndex++, memberMapBlock >>= 1)
                {
                    if ((memberMapBlock & 1) == 0)
                        continue;

                    var memberMap = allMemberMaps[memberMapIndex];
                    if (memberMap.IsReadOnly)
                    {
                        continue;
                    }

                    if (memberMap.IsRequired)
                    {
                        var fieldOrProperty = (memberMap.MemberInfo is FieldInfo) ? "field" : "property";
                        throw new FormatException($"Required element '{memberMap.ElementName}' for {fieldOrProperty} '{memberMap.MemberName}' of class {_classMap.ClassType.FullName} is missing.");
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
            }

            if (document != null)
            {
                if (supportsInitialization != null)
                {
                    supportsInitialization.EndInit();
                }
                return (TClass)document;
            }

            return CreateInstanceUsingCreator(values);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is BsonClassMapSerializer<TClass> other &&
                object.Equals(_classMap, other._classMap);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

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

            id = null;
            idNominalType = null;
            idGenerator = null;
            return false;
        }

        /// <summary>
        /// Tries to get the serialization info for a member.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="serializationInfo">The serialization information.</param>
        /// <returns>
        ///   <c>true</c> if the serialization info exists; otherwise <c>false</c>.
        /// </returns>
        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            for (var i = 0; i < _classMap.AllMemberMaps.Count; i++)
            {
                var memberMap = _classMap.AllMemberMaps[i];
                if (memberMap.MemberName == memberName)
                {
                    var elementName = memberMap.ElementName;
                    var serializer = memberMap.GetSerializer();
                    serializationInfo = new BsonSerializationInfo(elementName, serializer, serializer.ValueType);
                    return true;
                }
            }

            serializationInfo = null;
            return false;
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TClass value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
                return;
            }

            var actualType = value.GetType();
            if (actualType == typeof(TClass))
            {
                SerializeClass(context, args, value);
                return;
            }

            var actualTypeSerializer = this.GetSerializerForDerivedType(actualType);
            actualTypeSerializer.Serialize(context, args, value);
        }

        /// <summary>
        /// Sets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        public void SetDocumentId(object document, object id)
        {
            var documentType = document.GetType();
            var documentTypeInfo = documentType.GetTypeInfo();
            if (documentTypeInfo.IsValueType)
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
                throw new BsonSerializationException($"No matching creator found for class {_classMap.ClassType.FullName}.");
            }

            return creatorMap;
        }

        private TClass CreateInstanceUsingCreator(Dictionary<string, object> values)
        {
            var creatorMap = ChooseBestCreator(values);
            object document = creatorMap.CreateInstance(values); // removes values consumed
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

                if (memberMap == null)
                {
                    throw new FormatException($"Element '{elementName}' does not match any field or property of class {_classMap.ClassType.FullName}");
                }

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

        private void DeserializeExtraElementMember(
            BsonDeserializationContext context,
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

                var bsonValue = BsonValueSerializer.Instance.Deserialize(context);
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

                var bsonValue = BsonValueSerializer.Instance.Deserialize(context);
                extraElements[elementName] = BsonTypeMapper.MapToDotNetValue(bsonValue);
            }
        }

        private void DeserializeExtraElementValue(
            BsonDeserializationContext context,
            Dictionary<string, object> values,
            string elementName,
            BsonMemberMap extraElementsMemberMap)
        {
            if (extraElementsMemberMap.MemberType == typeof(BsonDocument))
            {
                BsonDocument extraElements;
                object obj;
                if (values.TryGetValue(extraElementsMemberMap.ElementName, out obj))
                {
                    extraElements = (BsonDocument)obj;
                }
                else
                {
                    extraElements = new BsonDocument();
                    values.Add(extraElementsMemberMap.ElementName, extraElements);
                }

                var bsonValue = BsonValueSerializer.Instance.Deserialize(context);
                extraElements[elementName] = bsonValue;
            }
            else
            {
                IDictionary<string, object> extraElements;
                object obj;
                if (values.TryGetValue(extraElementsMemberMap.ElementName, out obj))
                {
                    extraElements = (IDictionary<string, object>)obj;
                }
                else
                {
                    if (extraElementsMemberMap.MemberType == typeof(IDictionary<string, object>))
                    {
                        extraElements = new Dictionary<string, object>();
                    }
                    else
                    {
                        extraElements = (IDictionary<string, object>)Activator.CreateInstance(extraElementsMemberMap.MemberType);
                    }
                    values.Add(extraElementsMemberMap.ElementName, extraElements);
                }

                var bsonValue = BsonValueSerializer.Instance.Deserialize(context);
                extraElements[elementName] = BsonTypeMapper.MapToDotNetValue(bsonValue);
            }
        }

        private object DeserializeMemberValue(BsonDeserializationContext context, BsonMemberMap memberMap)
        {
            try
            {
                return memberMap.GetSerializer().Deserialize(context);
            }
            catch (Exception ex)
            {
                var message = string.Format(
                    "An error occurred while deserializing the {0} {1} of class {2}: {3}", // terminating period provided by nested message
                    memberMap.MemberName, (memberMap.MemberInfo is FieldInfo) ? "field" : "property", memberMap.ClassMap.ClassType.FullName, ex.Message);
                throw new FormatException(message, ex);
            }
        }

        private void SerializeClass(BsonSerializationContext context, BsonSerializationArgs args, TClass document)
        {
            var bsonWriter = context.Writer;

            bsonWriter.WriteStartDocument();

            var idMemberMap = _classMap.IdMemberMap;
            if (idMemberMap != null && args.SerializeIdFirst)
            {
                SerializeMember(context, document, idMemberMap);
            }

            if (ShouldSerializeDiscriminator(args.NominalType))
            {
                SerializeDiscriminator(context, args.NominalType, document);
            }

            for (var i = 0; i < _classMap.AllMemberMaps.Count; i++)
            {
                var memberMap = _classMap.AllMemberMaps[i];
                if (memberMap != idMemberMap || !args.SerializeIdFirst)
                {
                    SerializeMember(context, document, memberMap);
                }
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
                        BsonValueSerializer.Instance.Serialize(context, element.Value);
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
                        BsonValueSerializer.Instance.Serialize(context, bsonValue);
                    }
                }
            }
        }

        private void SerializeDiscriminator(BsonSerializationContext context, Type nominalType, object obj)
        {
            var discriminatorConvention = _classMap.GetDiscriminatorConvention();
            if (discriminatorConvention != null)
            {
                var actualType = obj.GetType();
                var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
                if (discriminator != null)
                {
                    context.Writer.WriteName(discriminatorConvention.ElementName);
                    BsonValueSerializer.Instance.Serialize(context, discriminator);
                }
            }
        }

        private void SerializeMember(BsonSerializationContext context, object obj, BsonMemberMap memberMap)
        {
            try
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
            catch (Exception ex)
            {
                var message = string.Format(
                    "An error occurred while serializing the {0} {1} of class {2}: {3}", // terminating period provided by nested message
                    memberMap.MemberName, (memberMap.MemberInfo is FieldInfo) ? "field" : "property", memberMap.ClassMap.ClassType.FullName, ex.Message);
                throw new BsonSerializationException(message, ex);
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
            memberMap.GetSerializer().Serialize(context, value);
        }

        private bool ShouldSerializeDiscriminator(Type nominalType)
        {
            return (nominalType != _classMap.ClassType || _classMap.DiscriminatorIsRequired || _classMap.HasRootClass) && !_classMap.IsAnonymous;
        }

        // nested classes
        // helper class that implements member map bit array helper functions
        internal static class FastMemberMapHelper
        {
            internal ref struct MembersBitArray()
            {
                private readonly ArrayPool<uint> _arrayPool;
                private readonly Span<uint> _bitArray;
                private readonly uint[] _rentedBuffer;
                private bool _isDisposed = false;

                public MembersBitArray(Span<uint> bitArray) : this()
                {
                    _arrayPool = null;
                    _bitArray = bitArray;
                    _rentedBuffer = null;

                    _bitArray.Clear();
                }

                public MembersBitArray(int lengthInUInts, ArrayPool<uint> arrayPool) : this()
                {
                    _arrayPool = arrayPool;
                    _rentedBuffer = arrayPool.Rent(lengthInUInts);
                    _bitArray = _rentedBuffer.AsSpan(0, lengthInUInts);

                    _bitArray.Clear();
                }

                public Span<uint> Span => _bitArray;
                public ArrayPool<uint> ArrayPool => _arrayPool;

                public void SetMemberIndex(int memberMapIndex) =>
                    _bitArray[memberMapIndex >> 5] |= 1U << (memberMapIndex & 31);

                public void Dispose()
                {
                    if (_isDisposed)
                        return;

                    if (_rentedBuffer != null)
                    {
                        _arrayPool.Return(_rentedBuffer);
                    }
                    _isDisposed = true;
                }
            }

            public static (int LengthInUInts, bool UseStackAlloc) GetLengthInUInts(int membersCount)
            {
                var lengthInUInts = (membersCount + 31) >> 5;
                return (lengthInUInts, lengthInUInts <= 8); // Use stackalloc for up to 256 members
            }

            public static MembersBitArray GetMembersBitArray(Span<uint> span) =>
                new(span);

            public static MembersBitArray GetMembersBitArray(int lengthInUInts) =>
                new(lengthInUInts, ArrayPool<uint>.Shared);
        }
    }
}
