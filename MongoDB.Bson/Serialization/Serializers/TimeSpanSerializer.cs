/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Timespans.
    /// </summary>
    public class TimeSpanSerializer : BsonBaseSerializer
    {
        // private static fields
        private static TimeSpanSerializer __instance = new TimeSpanSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the TimeSpanSerializer class.
        /// </summary>
        public TimeSpanSerializer()
            : base(new TimeSpanSerializationOptions(BsonType.String))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the TimeSpanSerializer class.
        /// </summary>
        public static TimeSpanSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            VerifyTypes(nominalType, actualType, typeof(TimeSpan));

            // support RepresentationSerializationOptions for backward compatibility
            var representationSerializationOptions = options as RepresentationSerializationOptions;
            if (representationSerializationOptions != null)
            {
                options = new TimeSpanSerializationOptions(representationSerializationOptions.Representation);
            }
            var timeSpanSerializationOptions = EnsureSerializationOptions<TimeSpanSerializationOptions>(options);

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.String)
            {
                return TimeSpan.Parse(bsonReader.ReadString()); // not XmlConvert.ToTimeSpan (we're using .NET's format for TimeSpan)
            }
            else if (timeSpanSerializationOptions.Units == TimeSpanUnits.Ticks)
            {
                long ticks;
                switch (bsonType)
                {
                    case BsonType.Double: ticks = (long)bsonReader.ReadDouble(); break;
                    case BsonType.Int32: ticks = (long)bsonReader.ReadInt32(); break;
                    case BsonType.Int64: ticks = bsonReader.ReadInt64(); break;
                    default:
                        var message = string.Format("Cannot deserialize TimeSpan from BsonType {0}.", bsonType);
                        throw new FileFormatException(message);
                }
                return new TimeSpan(ticks);
            }
            else
            {
                double interval;
                switch (bsonType)
                {
                    case BsonType.Double: interval = bsonReader.ReadDouble(); break;
                    case BsonType.Int32: interval = bsonReader.ReadInt32(); break;
                    case BsonType.Int64: interval = bsonReader.ReadInt64(); break;
                    default:
                        var message = string.Format("Cannot deserialize TimeSpan from BsonType {0}.", bsonType);
                        throw new FileFormatException(message);
                }

                switch (timeSpanSerializationOptions.Units)
                {
                    case TimeSpanUnits.Days: return TimeSpan.FromDays(interval);
                    case TimeSpanUnits.Hours: return TimeSpan.FromHours(interval);
                    case TimeSpanUnits.Minutes: return TimeSpan.FromMinutes(interval);
                    case TimeSpanUnits.Seconds: return TimeSpan.FromSeconds(interval);
                    case TimeSpanUnits.Milliseconds: return TimeSpan.FromMilliseconds(interval);
                    case TimeSpanUnits.Microseconds: return TimeSpan.FromTicks((long)interval*10L);
                    case TimeSpanUnits.Nanoseconds: return TimeSpan.FromMilliseconds(interval / 1000.0);
                    default:
                        var message = string.Format("'{0}' is not a valid TimeSpanUnits value.", timeSpanSerializationOptions.Units);
                        throw new BsonSerializationException(message);
                }
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options)
        {
            var timeSpan = (TimeSpan)value;

            // support RepresentationSerializationOptions for backward compatibility
            var representationSerializationOptions = options as RepresentationSerializationOptions;
            if (representationSerializationOptions != null)
            {
                options = new TimeSpanSerializationOptions(representationSerializationOptions.Representation);
            }
            var timeSpanSerializationOptions = EnsureSerializationOptions<TimeSpanSerializationOptions>(options);

            if (timeSpanSerializationOptions.Representation == BsonType.String)
            {
                bsonWriter.WriteString(timeSpan.ToString()); // for TimeSpan use .NET's format instead of XmlConvert.ToString
            }
            else if (timeSpanSerializationOptions.Units == TimeSpanUnits.Ticks)
            {
                var ticks = timeSpan.Ticks;
                switch (timeSpanSerializationOptions.Representation)
                {
                    case BsonType.Double: bsonWriter.WriteDouble((double)ticks); break;
                    case BsonType.Int32: bsonWriter.WriteInt32((int)ticks); break;
                    case BsonType.Int64: bsonWriter.WriteInt64(ticks); break;
                    default:
                        var message = string.Format("'{0}' is not a valid TimeSpan representation.", timeSpanSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
            else
            {
                double interval;
                switch (timeSpanSerializationOptions.Units)
                {
                    case TimeSpanUnits.Days: interval = timeSpan.TotalDays; break;
                    case TimeSpanUnits.Hours: interval = timeSpan.TotalHours; break;
                    case TimeSpanUnits.Minutes: interval = timeSpan.TotalMinutes; break;
                    case TimeSpanUnits.Seconds: interval = timeSpan.TotalSeconds; break;
                    case TimeSpanUnits.Milliseconds: interval = timeSpan.TotalMilliseconds; break;
                    case TimeSpanUnits.Microseconds: interval = timeSpan.Ticks / 10d; break;
                    case TimeSpanUnits.Nanoseconds: interval = timeSpan.TotalMilliseconds * 1000.0; break;
                    default:
                        var message = string.Format("'{0}' is not a valid TimeSpanUnits value.", timeSpanSerializationOptions.Units);
                        throw new BsonSerializationException(message);
                }

                switch (timeSpanSerializationOptions.Representation)
                {
                    case BsonType.Double: bsonWriter.WriteDouble(interval); break;
                    case BsonType.Int32: bsonWriter.WriteInt32((int)interval); break;
                    case BsonType.Int64: bsonWriter.WriteInt64((long)interval); break;
                    default:
                        var message = string.Format("'{0}' is not a valid TimeSpan representation.", timeSpanSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
    }
}
