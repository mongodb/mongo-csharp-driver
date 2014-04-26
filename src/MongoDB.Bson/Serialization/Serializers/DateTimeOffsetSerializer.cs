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
using System.IO;
using System.Xml;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for DateTimeOffsets.
    /// </summary>
    public class DateTimeOffsetSerializer : BsonBaseSerializer<DateTimeOffset>, IRepresentationConfigurable<DateTimeOffsetSerializer>
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetSerializer"/> class.
        /// </summary>
        public DateTimeOffsetSerializer()
            : this(BsonType.Array)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetSerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public DateTimeOffsetSerializer(BsonType representation)
        {
            switch (representation)
            {
                case BsonType.Array:
                case BsonType.Document:
                case BsonType.String:
                    break;

                default:
                    var message = string.Format("{0} is not a valid representation for a DateTimeOffsetSerializer.", representation);
                    throw new ArgumentException(message);
            }

            _representation = representation;
        }

        // public properties
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
        /// <returns>An object.</returns>
        public override DateTimeOffset Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            long ticks;
            TimeSpan offset;

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    ticks = bsonReader.ReadInt64();
                    offset = TimeSpan.FromMinutes(bsonReader.ReadInt32());
                    bsonReader.ReadEndArray();
                    return new DateTimeOffset(ticks, offset);

                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadDateTime("DateTime"); // ignore value
                    ticks = bsonReader.ReadInt64("Ticks");
                    offset = TimeSpan.FromMinutes(bsonReader.ReadInt32("Offset"));
                    bsonReader.ReadEndDocument();
                    return new DateTimeOffset(ticks, offset);

                case BsonType.String:
                    return XmlConvert.ToDateTimeOffset(bsonReader.ReadString());

                default:
                    var message = string.Format("Cannot deserialize DateTimeOffset from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, DateTimeOffset value)
        {
            var bsonWriter = context.Writer;

            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC

            switch (_representation)
            {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    bsonWriter.WriteInt64(value.Ticks);
                    bsonWriter.WriteInt32((int)value.Offset.TotalMinutes);
                    bsonWriter.WriteEndArray();
                    break;

                case BsonType.Document:
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteDateTime("DateTime", BsonUtils.ToMillisecondsSinceEpoch(value.UtcDateTime));
                    bsonWriter.WriteInt64("Ticks", value.Ticks);
                    bsonWriter.WriteInt32("Offset", (int)value.Offset.TotalMinutes);
                    bsonWriter.WriteEndDocument();
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(value));
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid DateTimeOffset representation.", _representation);
                    throw new BsonSerializationException(message);
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public DateTimeOffsetSerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new DateTimeOffsetSerializer(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
