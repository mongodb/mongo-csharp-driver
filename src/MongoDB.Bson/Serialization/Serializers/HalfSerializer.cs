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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
#if NET5_0_OR_GREATER
    /// <summary>
    /// Represents a serializer for Halfs.
    /// </summary>
    public sealed class HalfSerializer : StructSerializerBase<Half>, IRepresentationConfigurable<HalfSerializer>, IRepresentationConverterConfigurable<HalfSerializer>
    {
        #region static
        // private static fields
        private static readonly HalfSerializer __instance = new HalfSerializer();

        // public static properties
        /// <summary>
        /// Gets the default HalfSerializer.
        /// </summary>
        public static HalfSerializer Instance => __instance;
        #endregion

        // private fields
        private readonly BsonType _representation;
        private readonly RepresentationConverter _converter;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="HalfSerializer"/> class.
        /// </summary>
        public HalfSerializer()
            : this(BsonType.Double)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HalfSerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public HalfSerializer(BsonType representation)
            : this(representation, new RepresentationConverter(false, false))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HalfSerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <param name="converter">The converter.</param>
        public HalfSerializer(BsonType representation, RepresentationConverter converter)
        {
            switch (representation)
            {
                case BsonType.Decimal128:
                case BsonType.Double:
                case BsonType.Int32:
                case BsonType.Int64:
                case BsonType.String:
                    break;

                default:
                    throw new ArgumentException($"{representation} is not a valid representation for a HalfSerializer.");
            }

            _representation = representation;
            _converter = converter;
        }

        // public properties
        /// <inheritdoc/>
        public RepresentationConverter Converter => _converter;

        /// <inheritdoc/>
        public BsonType Representation => _representation;

        // public methods
        /// <inheritdoc/>
        public override Half Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Decimal128:
                    return _converter.ToHalf(bsonReader.ReadDecimal128());

                case BsonType.Double:
                    return _converter.ToHalf(bsonReader.ReadDouble());

                case BsonType.Int32:
                    return _converter.ToHalf(bsonReader.ReadInt32());

                case BsonType.Int64:
                    return _converter.ToHalf(bsonReader.ReadInt64());

                case BsonType.String:
                    return JsonConvert.ToHalf(bsonReader.ReadString());

                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is HalfSerializer other &&
                _converter.Equals(other._converter) &&
                _representation.Equals(other._representation);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Half value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Decimal128:
                    bsonWriter.WriteDecimal128(_converter.ToDecimal128(value));
                    break;

                case BsonType.Double:
                    bsonWriter.WriteDouble(_converter.ToDouble(value));
                    break;

                case BsonType.Int32:
                    bsonWriter.WriteInt32(_converter.ToInt32(value));
                    break;

                case BsonType.Int64:
                    bsonWriter.WriteInt64(_converter.ToInt64(value));
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(JsonConvert.ToString(value));
                    break;

                default:
                    throw new BsonSerializationException($"'{_representation}' is not a valid Half representation.");
            }
        }

        /// <inheritdoc/>
        public HalfSerializer WithConverter(RepresentationConverter converter)
        {
            return _converter.Equals(converter) ? this : new HalfSerializer(_representation, converter);
        }

        /// <inheritdoc/>
        public HalfSerializer WithRepresentation(BsonType representation)
        {
            return _representation == representation ? this : new HalfSerializer(representation);
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
#endif
}