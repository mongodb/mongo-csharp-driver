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

        // private fields
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
        /// <param name="units">The units.</param>
        public TimeOnlySerializer(BsonType representation, TimeOnlyUnits units)
        {
            switch (representation)
            {
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
                BsonType.String => TimeOnly.ParseExact(bsonReader.ReadString(), "o"),
                BsonType.Int64 =>  FromInt64(bsonReader.ReadInt64(), _units),
                BsonType.Int32 =>  FromInt32(bsonReader.ReadInt32(), _units),
                BsonType.Double =>  FromDouble(bsonReader.ReadDouble(), _units),
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