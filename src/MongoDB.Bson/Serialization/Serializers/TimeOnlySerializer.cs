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
    /// Represents a serializer for TimeOnlys
    /// </summary>
    public sealed class TimeOnlySerializer: StructSerializerBase<TimeOnly>, IRepresentationConfigurable<TimeOnlySerializer>
    {
        // static
        private static readonly TimeOnlySerializer __instance = new ();

        /// <summary>
        /// Gets the default TimeOnlySerializer
        /// </summary>
        public static TimeOnlySerializer Instance => __instance;

        // private constants
        private static class Flags
        {
            public const long Hour = 1;
            public const long Minute = 2;
            public const long Second = 4;
            public const long Millisecond = 8;
            public const long Microsecond = 16;
            public const long Nanosecond = 32;
            public const long Ticks = 64;
        }

        // private fields
        private readonly SerializerHelper _helper;
        private readonly BsonType _representation;
        private readonly TimeOnlyUnits _units;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnlySerializer"/> class.
        /// </summary>
        public TimeOnlySerializer()
            : this(BsonType.Int64, TimeOnlyUnits.Ticks)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnlySerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public TimeOnlySerializer(BsonType representation)
            : this(representation, TimeOnlyUnits.Ticks)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnlySerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <param name="units">The units. Ignored if representation is BsonType.Document.</param>
        public TimeOnlySerializer(BsonType representation, TimeOnlyUnits units)
        {
            switch (representation)
            {
                case BsonType.Document:
                case BsonType.Double:
                case BsonType.Int32:
                case BsonType.Int64:
                case BsonType.String:
                    break;

                default:
                    throw new ArgumentException($"{representation} is not a valid representation for a TimeOnlySerializer.");
            }

            _representation = representation;
            _units = units;

            _helper = new SerializerHelper
            (
                // TimeOnlySerializer was introduced in version 3.0.0 of the driver. Prior to that, TimeOnly was serialized
                // as a class mapped POCO. Due to that, Microsecond and Nanosecond could be missing, as they were introduced in .NET 7.
                // To avoid deserialization issues, we treat Microsecond and Nanosecond as optional members.
                new SerializerHelper.Member("Hour", Flags.Hour, isOptional: false),
                new SerializerHelper.Member("Minute", Flags.Minute, isOptional: false),
                new SerializerHelper.Member("Second", Flags.Second, isOptional: false),
                new SerializerHelper.Member("Millisecond", Flags.Millisecond, isOptional: false),
                new SerializerHelper.Member("Microsecond", Flags.Microsecond, isOptional: true),
                new SerializerHelper.Member("Nanosecond", Flags.Nanosecond, isOptional: true),
                new SerializerHelper.Member("Ticks", Flags.Ticks, isOptional: false)
            );
        }

        // public properties
        /// <inheritdoc />
        public BsonType Representation => _representation;

        /// <summary>
        /// Gets the units.
        /// </summary>
        /// <value>
        /// The units.
        /// </value>
        public TimeOnlyUnits Units => _units;

        // public methods
        /// <inheritdoc/>
        public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            var bsonType = bsonReader.GetCurrentBsonType();

            return bsonType switch
            {
                BsonType.Document => FromDocument(context),
                BsonType.Double =>  FromDouble(bsonReader.ReadDouble(), _units),
                BsonType.Int32 =>  FromInt32(bsonReader.ReadInt32(), _units),
                BsonType.Int64 =>  FromInt64(bsonReader.ReadInt64(), _units),
                BsonType.String => TimeOnly.ParseExact(bsonReader.ReadString(), "o"),
                _ => throw CreateCannotDeserializeFromBsonTypeException(bsonType)
            };
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }

            return
                base.Equals(obj) &&
                obj is TimeOnlySerializer other &&
                _representation.Equals(other._representation) &&
                _units.Equals(other._units);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Document:
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteInt32("Hour", value.Hour);
                    bsonWriter.WriteInt32("Minute", value.Minute);
                    bsonWriter.WriteInt32("Second", value.Second);
                    bsonWriter.WriteInt32("Millisecond", value.Millisecond);
                    // Microsecond and Nanosecond properties were added in .NET 7
                    bsonWriter.WriteInt32("Microsecond", GetMicrosecondsComponent(value.Ticks));
                    bsonWriter.WriteInt32("Nanosecond", GetNanosecondsComponent(value.Ticks));
                    bsonWriter.WriteInt64("Ticks", value.Ticks);
                    bsonWriter.WriteEndDocument();
                    break;

                case BsonType.Double:
                    bsonWriter.WriteDouble(ToDouble(value, _units));
                    break;

                case BsonType.Int32:
                    bsonWriter.WriteInt32(ToInt32(value, _units));
                    break;

                case BsonType.Int64:
                    bsonWriter.WriteInt64(ToInt64(value, _units));
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(value.ToString("o"));
                    break;

                default:
                    throw new BsonSerializationException($"'{_representation}' is not a valid TimeOnly representation.");
            }
        }

        /// <inheritdoc />
        public TimeOnlySerializer WithRepresentation(BsonType representation)
        {
            return representation == _representation ? this : new TimeOnlySerializer(representation);
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation and units.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <param name="units">The units.</param>
        /// <returns>
        /// The reconfigured serializer.
        /// </returns>
        public TimeOnlySerializer WithRepresentation(BsonType representation, TimeOnlyUnits units)
        {
            if (representation == _representation && units == _units)
            {
                return this;
            }

            return new TimeOnlySerializer(representation, units);
        }

        // private methods

        private TimeOnly FromDocument(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            var hour = 0;
            var minute = 0;
            var second = 0;
            var millisecond = 0;
            int? microsecond = null;
            int? nanosecond = null;
            var ticks = 0L;

            _helper.DeserializeMembers(context, (_, flag) =>
            {
                switch (flag)
                {
                    case Flags.Hour:
                        hour = bsonReader.ReadInt32();
                        break;
                    case Flags.Minute:
                        minute = bsonReader.ReadInt32();
                        break;
                    case Flags.Second:
                        second = bsonReader.ReadInt32();
                        break;
                    case Flags.Millisecond:
                        millisecond = bsonReader.ReadInt32();
                        break;
                    case Flags.Microsecond:
                        microsecond = bsonReader.ReadInt32();
                        break;
                    case Flags.Nanosecond:
                        nanosecond = bsonReader.ReadInt32();
                        break;
                    case Flags.Ticks:
                        ticks = bsonReader.ReadInt64();
                        break;
                }
            });

            var deserializedTimeOnly = new TimeOnly(ticks);

            if (deserializedTimeOnly.Hour != hour ||
                deserializedTimeOnly.Minute != minute ||
                deserializedTimeOnly.Second != second ||
                deserializedTimeOnly.Millisecond != millisecond ||
                (microsecond.HasValue && GetMicrosecondsComponent(deserializedTimeOnly.Ticks) != microsecond.Value) ||
                (nanosecond.HasValue && GetNanosecondsComponent(deserializedTimeOnly.Ticks) != nanosecond.Value))
            {
                throw new BsonSerializationException("Deserialized TimeOnly components do not match the ticks value.");
            }

            return deserializedTimeOnly;
        }

        private TimeOnly FromDouble(double value, TimeOnlyUnits units)
        {
            return units is TimeOnlyUnits.Nanoseconds
                ? new TimeOnly((long)(value / 100.0))
                : new TimeOnly((long)(value * TicksPerUnit(units)));
        }

        private TimeOnly FromInt32(int value, TimeOnlyUnits units)
        {
            return units is TimeOnlyUnits.Nanoseconds
                ? new TimeOnly(value / 100)
                : new TimeOnly(value * TicksPerUnit(units));
        }

        private TimeOnly FromInt64(long value, TimeOnlyUnits units)
        {
            return units is TimeOnlyUnits.Nanoseconds
                ? new TimeOnly(value / 100)
                : new TimeOnly(value * TicksPerUnit(units));
        }

        private int GetNanosecondsComponent(long ticks)
        {
            // ticks % 10 * 100
            return (int)(ticks % TicksPerUnit(TimeOnlyUnits.Microseconds) * 100);
        }

        private int GetMicrosecondsComponent(long ticks)
        {
            // ticks / 10 % 1000
            var ticksPerMicrosecond = TicksPerUnit(TimeOnlyUnits.Microseconds);
            return (int)(ticks / ticksPerMicrosecond % 1000);
        }

        private long TicksPerUnit(TimeOnlyUnits units)
        {
            return units switch
            {
                TimeOnlyUnits.Hours => TimeSpan.TicksPerHour,
                TimeOnlyUnits.Minutes => TimeSpan.TicksPerMinute,
                TimeOnlyUnits.Seconds => TimeSpan.TicksPerSecond,
                TimeOnlyUnits.Milliseconds => TimeSpan.TicksPerMillisecond,
                TimeOnlyUnits.Microseconds => TimeSpan.TicksPerMillisecond / 1000,
                TimeOnlyUnits.Ticks => 1,
                _ => throw new ArgumentException($"Invalid TimeOnlyUnits value: {units}.")
            };
        }

        private double ToDouble(TimeOnly timeOnly, TimeOnlyUnits units)
        {
            return units is TimeOnlyUnits.Nanoseconds
                ? timeOnly.Ticks * 100
                : timeOnly.Ticks / (double)TicksPerUnit(units);
        }

        private int ToInt32(TimeOnly timeOnly, TimeOnlyUnits units)
        {
            return units is TimeOnlyUnits.Nanoseconds
                ? (int)(timeOnly.Ticks * 100)
                : (int)(timeOnly.Ticks / TicksPerUnit(units));
        }

        private long ToInt64(TimeOnly timeOnly, TimeOnlyUnits units)
        {
            return units is TimeOnlyUnits.Nanoseconds
                ? timeOnly.Ticks * 100
                : timeOnly.Ticks / TicksPerUnit(units);
        }


        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
#endif
}