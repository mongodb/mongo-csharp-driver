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
        private readonly RepresentationConverter _converter;
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnlySerializer"/> class.
        /// </summary>
        public TimeOnlySerializer()
            : this(BsonType.Int64)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnlySerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public TimeOnlySerializer(BsonType representation)
        {
            switch (representation)
            {
                case BsonType.Int64:
                case BsonType.String:
                    break;

                default:
                    throw new ArgumentException($"{representation} is not a valid representation for a TimeOnlySerializer.");
            }

            _representation = representation;
            _converter = new RepresentationConverter(false, false);
        }

        // public properties
        /// <inheritdoc />
        public BsonType Representation => _representation;

        // public methods
        /// <inheritdoc/>
        public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            var bsonType = bsonReader.GetCurrentBsonType();

            return bsonType switch
            {
                BsonType.String => TimeOnly.ParseExact(bsonReader.ReadString(), "o"),
                BsonType.Int64 =>  new TimeOnly(bsonReader.ReadInt64()),
                BsonType.Int32 =>  new TimeOnly(bsonReader.ReadInt32()),
                BsonType.Double =>  new TimeOnly(_converter.ToInt64(bsonReader.ReadDouble())),
                BsonType.Decimal128 =>  new TimeOnly(_converter.ToInt64(bsonReader.ReadDecimal128())),
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
                _converter.Equals(other._converter);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Int64:
                    bsonWriter.WriteInt64(value.Ticks);
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

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
#endif
}