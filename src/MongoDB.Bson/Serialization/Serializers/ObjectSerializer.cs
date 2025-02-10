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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Shared;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for objects.
    /// </summary>
    public sealed class ObjectSerializer : ClassSerializerBase<object>, IHasDiscriminatorConvention
    {
        #region static
        // private static fields
        private static readonly Func<Type, bool> __allAllowedTypes = t => true;
        private static readonly ObjectSerializer __instance = new ObjectSerializer();
        private static readonly Func<Type, bool> __noAllowedTypes = t => false;

        // public static properties
        /// <summary>
        /// An allowed types function that returns true for all types.
        /// </summary>
        public static Func<Type, bool> AllAllowedTypes => __allAllowedTypes;

        /// <summary>
        /// An allowed types function that returns true for framework types known to be safe.
        /// </summary>
        public static Func<Type, bool> DefaultAllowedTypes => DefaultFrameworkAllowedTypes.AllowedTypes;

        /// <summary>
        /// Gets the standard instance.
        /// </summary>
        public static ObjectSerializer Instance => __instance;

        /// <summary>
        /// An allowed types function that returns false for all types.
        /// </summary>
        public static Func<Type, bool> NoAllowedTypes => __noAllowedTypes;
        #endregion

        // private fields
        private readonly Func<Type, bool> _allowedDeserializationTypes;
        private readonly Func<Type, bool> _allowedSerializationTypes;
        private readonly IDiscriminatorConvention _discriminatorConvention;
        private readonly GuidRepresentation _guidRepresentation;
        private readonly GuidSerializer _guidSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
        /// </summary>
        public ObjectSerializer()
            : this(BsonSerializer.LookupDiscriminatorConvention(typeof(object))) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <exception cref="System.ArgumentNullException">discriminatorConvention</exception>
        public ObjectSerializer(IDiscriminatorConvention discriminatorConvention)
            : this(discriminatorConvention, GuidRepresentation.Unspecified)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <param name="guidRepresentation">The Guid representation.</param>
        public ObjectSerializer(IDiscriminatorConvention discriminatorConvention, GuidRepresentation guidRepresentation)
            : this(discriminatorConvention, guidRepresentation, DefaultFrameworkAllowedTypes.AllowedTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
        /// </summary>
        /// <param name="allowedTypes">A delegate that determines what types are allowed.</param>
        public ObjectSerializer(Func<Type, bool> allowedTypes)
            : this(BsonSerializer.LookupDiscriminatorConvention(typeof(object)), allowedTypes) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <param name="allowedTypes">A delegate that determines what types are allowed.</param>
        public ObjectSerializer(IDiscriminatorConvention discriminatorConvention, Func<Type, bool> allowedTypes)
            : this(discriminatorConvention, GuidRepresentation.Unspecified, allowedTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <param name="guidRepresentation">The Guid representation.</param>
        /// <param name="allowedTypes">A delegate that determines what types are allowed.</param>
        public ObjectSerializer(IDiscriminatorConvention discriminatorConvention, GuidRepresentation guidRepresentation, Func<Type, bool> allowedTypes)
            : this(discriminatorConvention, guidRepresentation, allowedTypes ?? throw new ArgumentNullException(nameof(allowedTypes)), allowedTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <param name="guidRepresentation">The Guid representation.</param>
        /// <param name="allowedDeserializationTypes">A delegate that determines what types are allowed to be deserialized.</param>
        /// <param name="allowedSerializationTypes">A delegate that determines what types are allowed to be serialized.</param>
        public ObjectSerializer(
            IDiscriminatorConvention discriminatorConvention,
            GuidRepresentation guidRepresentation,
            Func<Type, bool> allowedDeserializationTypes,
            Func<Type, bool> allowedSerializationTypes)
        {
            _discriminatorConvention = discriminatorConvention ?? throw new ArgumentNullException(nameof(discriminatorConvention));
            _guidRepresentation = guidRepresentation;
            _guidSerializer = new GuidSerializer(_guidRepresentation);
            _allowedDeserializationTypes = allowedDeserializationTypes ?? throw new ArgumentNullException(nameof(allowedDeserializationTypes));
            _allowedSerializationTypes = allowedSerializationTypes ?? throw new ArgumentNullException(nameof(allowedSerializationTypes));
        }

        // public properties
        /// <summary>
        /// Gets the AllowedDeserializationTypes filter;
        /// </summary>
        public Func<Type, bool> AllowedDeserializationTypes => _allowedDeserializationTypes;

        /// <summary>
        /// Gets the AllowedSerializationTypes filter;
        /// </summary>
        public Func<Type, bool> AllowedSerializationTypes => _allowedSerializationTypes;

        /// <summary>
        /// Gets the discriminator convention.
        /// </summary>
        public IDiscriminatorConvention DiscriminatorConvention => _discriminatorConvention;

        /// <summary>
        /// Gets the GuidRepresentation.
        /// </summary>
        public GuidRepresentation GuidRepresentation => _guidRepresentation;

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Array:
                    if (context.DynamicArraySerializer != null)
                    {
                        return context.DynamicArraySerializer.Deserialize(context);
                    }
                    goto default;

                case BsonType.Binary:
                    var binaryDataBookmark = bsonReader.GetBookmark();
                    var binaryData = bsonReader.ReadBinaryData();
                    var subType = binaryData.SubType;
                    if (subType == BsonBinarySubType.UuidStandard || subType == BsonBinarySubType.UuidLegacy)
                    {
                        bsonReader.ReturnToBookmark(binaryDataBookmark);
                        return _guidSerializer.Deserialize(context);
                    }
                    goto default;

                case BsonType.Boolean:
                    return bsonReader.ReadBoolean();

                case BsonType.DateTime:
                    var millisecondsSinceEpoch = bsonReader.ReadDateTime();
                    var bsonDateTime = new BsonDateTime(millisecondsSinceEpoch);
                    return bsonDateTime.ToUniversalTime();

                case BsonType.Decimal128:
                    return bsonReader.ReadDecimal128();

                case BsonType.Document:
                    return DeserializeDiscriminatedValue(context, args);

                case BsonType.Double:
                    return bsonReader.ReadDouble();

                case BsonType.Int32:
                    return bsonReader.ReadInt32();

                case BsonType.Int64:
                    return bsonReader.ReadInt64();

                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.ObjectId:
                    return bsonReader.ReadObjectId();

                case BsonType.String:
                    return bsonReader.ReadString();

                default:
                    var message = string.Format("ObjectSerializer does not support BSON type '{0}'.", bsonType);
                    throw new FormatException(message);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ObjectSerializer other &&
                object.Equals(_allowedDeserializationTypes, other._allowedDeserializationTypes) &&
                object.Equals(_allowedSerializationTypes, other._allowedSerializationTypes) &&
                object.Equals(_discriminatorConvention, other._discriminatorConvention) &&
                _guidRepresentation.Equals(other._guidRepresentation);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                if (actualType == typeof(object))
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteEndDocument();
                }
                else
                {
                    // certain types can be written directly as BSON value
                    // if we're not at the top level document, or if we're using the JsonWriter
                    if (bsonWriter.State == BsonWriterState.Value || bsonWriter is JsonWriter)
                    {
                        switch (Type.GetTypeCode(actualType))
                        {
                            case TypeCode.Boolean:
                                bsonWriter.WriteBoolean((bool)value);
                                return;

                            case TypeCode.DateTime:
                                // TODO: is this right? will lose precision after round trip
                                var bsonDateTime = new BsonDateTime(BsonUtils.ToUniversalTime((DateTime)value));
                                bsonWriter.WriteDateTime(bsonDateTime.MillisecondsSinceEpoch);
                                return;

                            case TypeCode.Double:
                                bsonWriter.WriteDouble((double)value);
                                return;

                            case TypeCode.Int16:
                                // TODO: is this right? will change type to Int32 after round trip
                                bsonWriter.WriteInt32((short)value);
                                return;

                            case TypeCode.Int32:
                                bsonWriter.WriteInt32((int)value);
                                return;

                            case TypeCode.Int64:
                                bsonWriter.WriteInt64((long)value);
                                return;

                            case TypeCode.Object:
                                if (actualType == typeof(Decimal128))
                                {
                                    var decimal128 = (Decimal128)value;
                                    bsonWriter.WriteDecimal128(decimal128);
                                    return;
                                }
                                if (actualType == typeof(Guid))
                                {
                                    var guid = (Guid)value;
                                    _guidSerializer.Serialize(context, args, guid);
                                    return;
                                }
                                if (actualType == typeof(ObjectId))
                                {
                                    bsonWriter.WriteObjectId((ObjectId)value);
                                    return;
                                }
                                break;

                            case TypeCode.String:
                                bsonWriter.WriteString((string)value);
                                return;
                        }
                    }

                    SerializeDiscriminatedValue(context, args, value, actualType);
                }
            }
        }

        /// <summary>
        /// Returns a new ObjectSerializer configured the same but with the specified discriminator convention.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <returns>An ObjectSerializer with the specified discriminator convention.</returns>
        public ObjectSerializer WithDiscriminatorConvention(IDiscriminatorConvention discriminatorConvention)
        {
            return new ObjectSerializer(discriminatorConvention, _guidRepresentation, _allowedDeserializationTypes, _allowedSerializationTypes);
        }

        /// <summary>
        /// Returns a new ObjectSerializer configured the same but with the specified allowed types delegates.
        /// </summary>
        /// <param name="allowedDeserializationTypes">A delegate that determines what types are allowed to be deserialized.</param>
        /// <param name="allowedSerializationTypes">A delegate that determines what types are allowed to be serialized.</param>
        /// <returns></returns>
        public ObjectSerializer WithAllowedTypes(Func<Type, bool> allowedDeserializationTypes, Func<Type, bool> allowedSerializationTypes)
        {
            return new ObjectSerializer(_discriminatorConvention, _guidRepresentation, allowedDeserializationTypes, allowedSerializationTypes);
        }

        // private methods
        private object DeserializeDiscriminatedValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            var actualType = _discriminatorConvention.GetActualType(bsonReader, typeof(object));
            if (!_allowedDeserializationTypes(actualType))
            {
                throw new BsonSerializationException($"Type {actualType.FullName} is not configured as a type that is allowed to be deserialized for this instance of ObjectSerializer.");
            }

            if (actualType == typeof(object))
            {
                var type = bsonReader.GetCurrentBsonType();
                switch (type)
                {
                    case BsonType.Document:
                        if (context.DynamicDocumentSerializer != null)
                        {
                            return context.DynamicDocumentSerializer.Deserialize(context, args);
                        }
                        break;
                }

                bsonReader.ReadStartDocument();
                bsonReader.ReadEndDocument();
                return new object();
            }
            else
            {
                var serializer = context.SerializationDomain.LookupSerializer(actualType);
                var polymorphicSerializer = serializer as IBsonPolymorphicSerializer;
                if (polymorphicSerializer != null && polymorphicSerializer.IsDiscriminatorCompatibleWithObjectSerializer)
                {
                    return serializer.Deserialize(context, args);
                }
                else
                {
                    object value = null;
                    var wasValuePresent = false;

                    bsonReader.ReadStartDocument();
                    while (bsonReader.ReadBsonType() != 0)
                    {
                        var name = bsonReader.ReadName();
                        if (name == _discriminatorConvention.ElementName)
                        {
                            bsonReader.SkipValue();
                        }
                        else if (name == "_v")
                        {
                            value = serializer.Deserialize(context);
                            wasValuePresent = true;
                        }
                        else
                        {
                            var message = string.Format("Unexpected element name: '{0}'.", name);
                            throw new FormatException(message);
                        }
                    }
                    bsonReader.ReadEndDocument();

                    if (!wasValuePresent)
                    {
                        throw new FormatException("_v element missing.");
                    }

                    return value;
                }
            }
        }

        private void SerializeDiscriminatedValue(BsonSerializationContext context, BsonSerializationArgs args, object value, Type actualType)
        {
            if (!_allowedSerializationTypes(actualType))
            {
                throw new BsonSerializationException($"Type {actualType.FullName} is not configured as a type that is allowed to be serialized for this instance of ObjectSerializer.");
            }

            var serializer = context.SerializationDomain.LookupSerializer(actualType);

            var polymorphicSerializer = serializer as IBsonPolymorphicSerializer;
            if (polymorphicSerializer != null && polymorphicSerializer.IsDiscriminatorCompatibleWithObjectSerializer)
            {
                serializer.Serialize(context, args, value);
            }
            else
            {
                if (context.IsDynamicType != null && context.IsDynamicType(value.GetType()))
                {
                    args.NominalType = actualType;
                    serializer.Serialize(context, args, value);
                }
                else
                {
                    var bsonWriter = context.Writer;
                    var discriminator = _discriminatorConvention.GetDiscriminator(typeof(object), actualType);

                    bsonWriter.WriteStartDocument();
                    if (discriminator != null)
                    {
                        bsonWriter.WriteName(_discriminatorConvention.ElementName);
                        BsonValueSerializer.Instance.Serialize(context, discriminator);
                    }
                    bsonWriter.WriteName("_v");
                    serializer.Serialize(context, value);
                    bsonWriter.WriteEndDocument();
                }
            }
        }

        // nested types
        private static class DefaultFrameworkAllowedTypes
        {
            private readonly static Func<Type, bool> __allowedTypes = AllowedTypesImplementation;

            private readonly static HashSet<Type> __allowedNonGenericTypesSet = new HashSet<Type>
            {
                typeof(System.Boolean),
                typeof(System.Byte),
                typeof(System.Char),
                typeof(System.Collections.ArrayList),
                typeof(System.Collections.BitArray),
                typeof(System.Collections.Hashtable),
                typeof(System.Collections.Queue),
                typeof(System.Collections.SortedList),
                typeof(System.Collections.Specialized.ListDictionary),
                typeof(System.Collections.Specialized.OrderedDictionary),
                typeof(System.Collections.Stack),
                typeof(System.DateTime),
                typeof(System.DateTimeOffset),
                typeof(System.Decimal),
                typeof(System.Double),
                typeof(System.Dynamic.ExpandoObject),
                typeof(System.Guid),
                typeof(System.Int16),
                typeof(System.Int32),
                typeof(System.Int64),
                typeof(System.Net.DnsEndPoint),
                typeof(System.Net.EndPoint),
                typeof(System.Net.IPAddress),
                typeof(System.Net.IPEndPoint),
                typeof(System.Net.IPHostEntry),
                typeof(System.Object),
                typeof(System.SByte),
                typeof(System.Single),
                typeof(System.String),
                typeof(System.Text.RegularExpressions.Regex),
                typeof(System.TimeSpan),
                typeof(System.UInt16),
                typeof(System.UInt32),
                typeof(System.UInt64),
                typeof(System.Uri),
                typeof(System.Version)
            };

            private readonly static HashSet<Type> __allowedGenericTypesSet = new HashSet<Type>
            {
                typeof(System.Collections.Generic.Dictionary<,>),
                typeof(System.Collections.Generic.HashSet<>),
                typeof(System.Collections.Generic.KeyValuePair<,>),
                typeof(System.Collections.Generic.LinkedList<>),
                typeof(System.Collections.Generic.List<>),
                typeof(System.Collections.Generic.Queue<>),
                typeof(System.Collections.Generic.SortedDictionary<,>),
                typeof(System.Collections.Generic.SortedList<,>),
                typeof(System.Collections.Generic.SortedSet<>),
                typeof(System.Collections.Generic.Stack<>),
                typeof(System.Collections.ObjectModel.Collection<>),
                typeof(System.Collections.ObjectModel.KeyedCollection<,>),
                typeof(System.Collections.ObjectModel.ObservableCollection<>),
                typeof(System.Collections.ObjectModel.ReadOnlyCollection<>),
                typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>),
                typeof(System.Collections.ObjectModel.ReadOnlyObservableCollection<>),
                typeof(System.Nullable<>),
                typeof(System.Tuple<>),
                typeof(System.Tuple<,>),
                typeof(System.Tuple<,,>),
                typeof(System.Tuple<,,,>),
                typeof(System.Tuple<,,,,>),
                typeof(System.Tuple<,,,,,>),
                typeof(System.Tuple<,,,,,,>),
                typeof(System.Tuple<,,,,,,,>),
                typeof(System.ValueTuple<,,,,,,,>),
                typeof(System.ValueTuple<>),
                typeof(System.ValueTuple<,>),
                typeof(System.ValueTuple<,,>),
                typeof(System.ValueTuple<,,,>),
                typeof(System.ValueTuple<,,,,>),
                typeof(System.ValueTuple<,,,,,>),
                typeof(System.ValueTuple<,,,,,,>),
                typeof(System.ValueTuple<,,,,,,,>)
            };

            public static Func<Type, bool> AllowedTypes => __allowedTypes;

            private static bool AllowedTypesImplementation(Type type)
            {
                return type.IsConstructedGenericType ? IsAllowedGenericType(type) : IsAllowedType(type);

                static bool IsAllowedType(Type type) =>
                    typeof(BsonValue).IsAssignableFrom(type) ||
                    __allowedNonGenericTypesSet.Contains(type) ||
                    type.IsArray && AllowedTypesImplementation(type.GetElementType()) ||
                    type.IsEnum;

                static bool IsAllowedGenericType(Type type) =>
                    (__allowedGenericTypesSet.Contains(type.GetGenericTypeDefinition()) || type.IsAnonymousType()) &&
                    type.GetGenericArguments().All(__allowedTypes);
            }
        }
    }
}
