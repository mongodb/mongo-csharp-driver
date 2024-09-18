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
#if NET6_0_OR_GREATER
    /// <summary>
    /// Represents a serializer for DateOnlys.
    /// </summary>
    public sealed class DateOnlySerializer : StructSerializerBase<DateOnly>, IRepresentationConfigurable<DateOnlySerializer>
    {
        // static
        private static readonly DateOnlySerializer __instance = new DateOnlySerializer();

        /// <summary>
        /// Gets the default DateOnlySerializer.
        /// </summary>
        public static DateOnlySerializer Instance => __instance;

        // private constants
        private static class Flags
        {
            public const long DateTime = 1;
            public const long Ticks = 2;
        }

        // private fields
        private readonly RepresentationConverter _converter;
        private readonly SerializerHelper _helper;
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DateOnlySerializer"/> class.
        /// </summary>
        public DateOnlySerializer()
            : this(BsonType.DateTime)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateOnlySerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public DateOnlySerializer(BsonType representation)
        {
            switch (representation)
            {
                case BsonType.DateTime:
                case BsonType.Document:
                case BsonType.Int64:
                case BsonType.String:
                    break;

                default:
                    throw new ArgumentException($"{representation} is not a valid representation for a DateOnlySerializer.");
            }

            _representation = representation;
            _converter = new RepresentationConverter(false, false);

            _helper = new SerializerHelper
            (
                new SerializerHelper.Member("DateTime", Flags.DateTime),
                new SerializerHelper.Member("Ticks", Flags.Ticks)
            );
        }

        // public properties
        /// <inheritdoc />
        public BsonType Representation => _representation;

        //public methods
        /// <inheritdoc />
        public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            DateOnly value;

            var bsonType = bsonReader.GetCurrentBsonType();

            switch (bsonType)
            {
                case BsonType.DateTime:
                    value = VerifyAndMakeDateOnly(BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonReader.ReadDateTime()));
                    break;

                case BsonType.Document:
                    value = default;
                    _helper.DeserializeMembers(context, (_, flag) =>
                    {
                        switch (flag)
                        {
                            case Flags.DateTime: bsonReader.SkipValue(); break; // ignore value (use Ticks instead)
                            case Flags.Ticks:
                                value = VerifyAndMakeDateOnly(new DateTime(Int64Serializer.Instance.Deserialize(context), DateTimeKind.Utc));
                                break;
                        }
                    });
                    break;

                case BsonType.Decimal128:
                    value = VerifyAndMakeDateOnly(new DateTime(_converter.ToInt64(bsonReader.ReadDecimal128()), DateTimeKind.Utc));
                    break;

                case BsonType.Double:
                    value = VerifyAndMakeDateOnly(new DateTime(_converter.ToInt64(bsonReader.ReadDouble()), DateTimeKind.Utc));
                    break;

                case BsonType.Int32:
                    value = VerifyAndMakeDateOnly(new DateTime(bsonReader.ReadInt32(), DateTimeKind.Utc));
                    break;

                case BsonType.Int64:
                    value = VerifyAndMakeDateOnly(new DateTime(bsonReader.ReadInt64(), DateTimeKind.Utc));
                    break;

                case BsonType.String:
                    value = DateOnly.ParseExact(bsonReader.ReadString(), "yyyy-MM-dd");
                    break;

                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }

            return value;

            DateOnly VerifyAndMakeDateOnly(DateTime dt)
            {
                if (dt.TimeOfDay != TimeSpan.Zero)
                {
                    throw new FormatException("Deserialized value has a non-zero time component.");
                }

                return DateOnly.FromDateTime(dt);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is DateOnlySerializer other &&
                _representation.Equals(other._representation);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
        {
            var bsonWriter = context.Writer;

            var utcDateTime = value.ToDateTime(new TimeOnly(0), DateTimeKind.Utc);
            var millisecondsSinceEpoch = BsonUtils.ToMillisecondsSinceEpoch(utcDateTime);

            switch (_representation)
            {
                case BsonType.DateTime:
                    bsonWriter.WriteDateTime(millisecondsSinceEpoch);
                    break;

                case BsonType.Document:
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteDateTime("DateTime", millisecondsSinceEpoch);
                    bsonWriter.WriteInt64("Ticks", utcDateTime.Ticks);
                    bsonWriter.WriteEndDocument();
                    break;

                case BsonType.Int64:
                    bsonWriter.WriteInt64(utcDateTime.Ticks);
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(value.ToString("yyyy-MM-dd"));
                    break;

                default:
                    throw new BsonSerializationException($"'{_representation}' is not a valid DateOnly representation.");
            }
        }

        /// <inheritdoc />
        public DateOnlySerializer WithRepresentation(BsonType representation)
        {
            return representation == _representation ? this : new DateOnlySerializer(representation);
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
#endif
}

