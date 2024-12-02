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
using MongoDB.Bson.Serialization.Attributes;
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
        private static readonly DateOnlySerializer __instance = new();

        /// <summary>
        /// Gets the default DateOnlySerializer.
        /// </summary>
        public static DateOnlySerializer Instance => __instance;

        // private constants
        private static class Flags
        {
            public const long DateTime = 1;
            public const long Ticks = 2;
            public const long Year = 4;
            public const long Month = 8;
            public const long Day = 16;

            public const long DateTimeTicks = DateTime | Ticks;
            public const long YearMonthDay = Year | Month | Day;
        }

        // private fields
        private readonly RepresentationConverter _converter;
        private readonly SerializerHelper _helper;
        private readonly BsonType _representation;
        private readonly DateOnlyDocumentFormat _documentFormat;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DateOnlySerializer"/> class.
        /// </summary>
        public DateOnlySerializer()
            : this(BsonType.DateTime, DateOnlyDocumentFormat.DateTimeTicks)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateOnlySerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public DateOnlySerializer(BsonType representation)
            : this(representation, DateOnlyDocumentFormat.DateTimeTicks)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateOnlySerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <param name="documentFormat">The format to use with the BsonType.Document representation. It will be ignored if the representation is different.</param>
        public DateOnlySerializer(BsonType representation, DateOnlyDocumentFormat documentFormat)
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
            _documentFormat = documentFormat;
            _converter = new RepresentationConverter(false, false);
            _helper = new SerializerHelper
            (
                new SerializerHelper.Member("DateTime", Flags.DateTime, isOptional: true),
                new SerializerHelper.Member("Ticks", Flags.Ticks, isOptional: true),
                new SerializerHelper.Member("Year", Flags.Year, isOptional: true),
                new SerializerHelper.Member("Month", Flags.Month, isOptional: true),
                new SerializerHelper.Member("Day", Flags.Day, isOptional: true)
            );
        }

        // public properties
        /// <inheritdoc />
        public BsonType Representation => _representation;

        /// <summary>
        /// The format to use for the BsonType.Document representation. It will be ignored if the representation is different.
        /// </summary>
        public DateOnlyDocumentFormat DocumentFormat => _documentFormat;

        //public methods
        /// <inheritdoc />
        public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();

            switch (bsonType)
            {
                case BsonType.DateTime:
                    return VerifyAndMakeDateOnly(BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonReader.ReadDateTime()));

                case BsonType.Document:
                    var ticks = 0L;
                    var year = 0;
                    var month = 0;
                    var day = 0;

                    var foundMemberFlags = _helper.DeserializeMembers(context, (_, flag) =>
                    {
                        switch (flag)
                        {
                            case Flags.DateTime: bsonReader.SkipValue();  break; // ignore value (use Ticks instead)
                            case Flags.Ticks: ticks = Int64Serializer.Instance.Deserialize(context); break;
                            case Flags.Year: year = Int32Serializer.Instance.Deserialize(context); break;
                            case Flags.Month: month = Int32Serializer.Instance.Deserialize(context); break;
                            case Flags.Day: day = Int32Serializer.Instance.Deserialize(context); break;
                        }
                    });

                    return foundMemberFlags switch
                    {
                        Flags.DateTimeTicks => VerifyAndMakeDateOnly(new DateTime(ticks, DateTimeKind.Utc)),
                        Flags.YearMonthDay => new DateOnly(year, month, day),
                        _ => throw new FormatException("Invalid document format.")
                    };

                case BsonType.Decimal128:
                    return VerifyAndMakeDateOnly(new DateTime(_converter.ToInt64(bsonReader.ReadDecimal128()), DateTimeKind.Utc));

                case BsonType.Double:
                    return VerifyAndMakeDateOnly(new DateTime(_converter.ToInt64(bsonReader.ReadDouble()), DateTimeKind.Utc));

                case BsonType.Int32:
                    return VerifyAndMakeDateOnly(new DateTime(bsonReader.ReadInt32(), DateTimeKind.Utc));

                case BsonType.Int64:
                    return VerifyAndMakeDateOnly(new DateTime(bsonReader.ReadInt64(), DateTimeKind.Utc));

                case BsonType.String:
                    return DateOnly.ParseExact(bsonReader.ReadString(), "yyyy-MM-dd");

                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }


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
                _representation.Equals(other._representation) &&
                _documentFormat.Equals(other._documentFormat);
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
                    if (_documentFormat is DateOnlyDocumentFormat.DateTimeTicks)
                    {
                        bsonWriter.WriteDateTime("DateTime", millisecondsSinceEpoch);
                        bsonWriter.WriteInt64("Ticks", utcDateTime.Ticks);
                    }
                    else
                    {
                        bsonWriter.WriteInt32("Year", value.Year);
                        bsonWriter.WriteInt32("Month", value.Month);
                        bsonWriter.WriteInt32("Day", value.Day);
                    }
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

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation and document format.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <param name="documentFormat">The document format to use with BsonType.Document representation.</param>
        /// <returns>
        /// The reconfigured serializer.
        /// </returns>
        public DateOnlySerializer WithRepresentation(BsonType representation, DateOnlyDocumentFormat documentFormat)
        {
            if (representation == _representation && documentFormat == _documentFormat)
            {
                return this;
            }

            return new DateOnlySerializer(representation, documentFormat);
        }

        /// <inheritdoc />
        public DateOnlySerializer WithRepresentation(BsonType representation)
        {
            return representation == _representation ? this : new DateOnlySerializer(representation, _documentFormat);
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
#endif
}

