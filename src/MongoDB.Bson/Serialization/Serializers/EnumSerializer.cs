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
using System.Reflection;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for enums.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enum.</typeparam>
    public class EnumSerializer<TEnum> : StructSerializerBase<TEnum>, IRepresentationConverterConfigurable<EnumSerializer<TEnum>>, IRepresentationConfigurable<EnumSerializer<TEnum>> where TEnum : struct
    {
        // private fields
        private readonly BsonType _representation;
        private readonly RepresentationConverter _converter;
        private readonly TypeCode _underlyingTypeCode;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSerializer{TEnum}"/> class.
        /// </summary>
        public EnumSerializer()
            : this((BsonType)0) // 0 means use underlying type
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSerializer{TEnum}"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public EnumSerializer(BsonType representation)
            : this(representation, new RepresentationConverter(false, false))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSerializer{TEnum}"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <param name="converter">The converter.</param>
        public EnumSerializer(BsonType representation, RepresentationConverter converter)
        {
            switch (representation)
            {
                case 0:
                case BsonType.Int32:
                case BsonType.Int64:
                case BsonType.String:
                    break;

                default:
                    var message = string.Format("{0} is not a valid representation for an EnumSerializer.", representation);
                    throw new ArgumentException(message);
            }

            // don't know of a way to enforce this at compile time
            var enumTypeInfo = typeof(TEnum).GetTypeInfo();
            if (!enumTypeInfo.IsEnum)
            {
                var message = string.Format("{0} is not an enum type.", typeof(TEnum).FullName);
                throw new BsonSerializationException(message);
            }

            _representation = representation;
            _converter = converter;
            _underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum)));
        }

        // public properties
        /// <summary>
        /// Gets the converter.
        /// </summary>
        /// <value>
        /// The converter.
        /// </value>
        public RepresentationConverter Converter
        {
            get { return _converter; }
        }

        /// <summary>
        /// Gets the representation.
        /// </summary>
        /// <value>
        /// The representation.
        /// </value>
        public BsonType Representation
        {
            get { return _representation; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Int32: return (TEnum)Enum.ToObject(typeof(TEnum), bsonReader.ReadInt32());
                case BsonType.Int64: return (TEnum)Enum.ToObject(typeof(TEnum), bsonReader.ReadInt64());
                case BsonType.Double: return (TEnum)Enum.ToObject(typeof(TEnum), (long)bsonReader.ReadDouble());
                case BsonType.String: return (TEnum)Enum.Parse(typeof(TEnum), bsonReader.ReadString());
                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case 0:
                    if (_underlyingTypeCode == TypeCode.Int64 || _underlyingTypeCode == TypeCode.UInt64)
                    {
                        goto case BsonType.Int64;
                    }
                    else
                    {
                        goto case BsonType.Int32;
                    }

                case BsonType.Int32:
                    bsonWriter.WriteInt32(_converter.ToInt32(Convert.ToInt64(value))); // we could be dealing with overflowing UInt32s here so we convert to Int64 first
                    break;

                case BsonType.Int64:
                    bsonWriter.WriteInt64(Convert.ToInt64(value));
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(value.ToString());
                    break;

                default:
                    throw new BsonInternalException("Unexpected EnumRepresentation.");
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="converter">The converter.</param>
        /// <returns>The reconfigured serializer.</returns>
        public EnumSerializer<TEnum> WithConverter(RepresentationConverter converter)
        {
            if (converter == _converter)
            {
                return this;
            }
            else
            {
                return new EnumSerializer<TEnum>(_representation, converter);
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public EnumSerializer<TEnum> WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new EnumSerializer<TEnum>(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConverterConfigurable.WithConverter(RepresentationConverter converter)
        {
            return WithConverter(converter);
        }

        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
