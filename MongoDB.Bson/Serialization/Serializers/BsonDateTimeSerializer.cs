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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BsonDateTimes.
    /// </summary>
    public class BsonDateTimeSerializer : BsonBaseSerializer
    {
        // private static fields
        private static BsonDateTimeSerializer __instance = new BsonDateTimeSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDateTimeSerializer class.
        /// </summary>
        public BsonDateTimeSerializer()
            : base(DateTimeSerializationOptions.Defaults)
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonDateTimeSerializer class.
        /// </summary>
        public static BsonDateTimeSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(BsonDateTime));
            var dateTimeSerializationOptions = EnsureSerializationOptions<DateTimeSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                long? millisecondsSinceEpoch = null;
                long? ticks = null;
                switch (bsonType)
                {
                    case BsonType.DateTime:
                        millisecondsSinceEpoch = bsonReader.ReadDateTime();
                        break;
                    case BsonType.Document:
                        bsonReader.ReadStartDocument();
                        millisecondsSinceEpoch = bsonReader.ReadDateTime("DateTime");
                        bsonReader.ReadName("Ticks");
                        var ticksValue = BsonValue.ReadFrom(bsonReader);
                        if (!ticksValue.IsBsonUndefined)
                        {
                            ticks = ticksValue.ToInt64();
                        }
                        bsonReader.ReadEndDocument();
                        break;
                    case BsonType.Int64:
                        ticks = bsonReader.ReadInt64();
                        break;
                    case BsonType.String:
                        // note: we're not using XmlConvert because of bugs in Mono
                        DateTime dateTime;
                        if (dateTimeSerializationOptions.DateOnly)
                        {
                            dateTime = DateTime.SpecifyKind(DateTime.ParseExact(bsonReader.ReadString(), "yyyy-MM-dd", null), DateTimeKind.Utc);
                        }
                        else
                        {
                            var formats = new string[] { "yyyy-MM-ddK", "yyyy-MM-ddTHH:mm:ssK", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", };
                            dateTime = DateTime.ParseExact(bsonReader.ReadString(), formats, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                        }
                        ticks = dateTime.Ticks;
                        break;
                    default:
                        var message = string.Format("Cannot deserialize DateTime from BsonType {0}.", bsonType);
                        throw new FileFormatException(message);
                }

                BsonDateTime bsonDateTime;
                if (ticks.HasValue)
                {
                    bsonDateTime = new BsonDateTime(new DateTime(ticks.Value, DateTimeKind.Utc));
                }
                else
                {
                    bsonDateTime = new BsonDateTime(millisecondsSinceEpoch.Value);
                }

                if (dateTimeSerializationOptions.DateOnly)
                {
                    var dateTime = bsonDateTime.ToUniversalTime();
                    if (dateTime.TimeOfDay != TimeSpan.Zero)
                    {
                        throw new FileFormatException("TimeOfDay component for DateOnly DateTime value is not zero.");
                    }
                    bsonDateTime = new BsonDateTime(DateTime.SpecifyKind(dateTime, dateTimeSerializationOptions.Kind)); // not ToLocalTime or ToUniversalTime!
                }
                else
                {
                    if (bsonDateTime.IsValidDateTime)
                    {
                        var dateTime = bsonDateTime.ToUniversalTime();
                        switch (dateTimeSerializationOptions.Kind)
                        {
                            case DateTimeKind.Local:
                            case DateTimeKind.Unspecified:
                                dateTime = DateTime.SpecifyKind(BsonUtils.ToLocalTime(dateTime), dateTimeSerializationOptions.Kind);
                                break;
                            case DateTimeKind.Utc:
                                dateTime = BsonUtils.ToUniversalTime(dateTime);
                                break;
                        }
                        bsonDateTime = new BsonDateTime(dateTime);
                    }
                    else
                    {
                        if (dateTimeSerializationOptions.Kind != DateTimeKind.Utc)
                        {
                            throw new FileFormatException("BsonDateTime is outside the range of .NET DateTime.");
                        }
                    }
                }

                return bsonDateTime;
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
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var bsonDateTime = (BsonDateTime)value;
                var dateTimeSerializationOptions = EnsureSerializationOptions<DateTimeSerializationOptions>(options);

                DateTime utcDateTime = DateTime.MinValue;
                long millisecondsSinceEpoch;
                if (dateTimeSerializationOptions.DateOnly)
                {
                    if (bsonDateTime.ToUniversalTime().TimeOfDay != TimeSpan.Zero)
                    {
                        throw new BsonSerializationException("TimeOfDay component is not zero.");
                    }
                    utcDateTime = DateTime.SpecifyKind(bsonDateTime.ToUniversalTime(), DateTimeKind.Utc); // not ToLocalTime
                    millisecondsSinceEpoch = BsonUtils.ToMillisecondsSinceEpoch(utcDateTime);
                }
                else
                {
                    if (bsonDateTime.IsValidDateTime)
                    {
                        utcDateTime = BsonUtils.ToUniversalTime(bsonDateTime.ToUniversalTime());
                    }
                    millisecondsSinceEpoch = bsonDateTime.MillisecondsSinceEpoch;
                }

                switch (dateTimeSerializationOptions.Representation)
                {
                    case BsonType.DateTime:
                        bsonWriter.WriteDateTime(millisecondsSinceEpoch);
                        break;
                    case BsonType.Document:
                        bsonWriter.WriteStartDocument();
                        bsonWriter.WriteDateTime("DateTime", millisecondsSinceEpoch);
                        if (bsonDateTime.IsValidDateTime)
                        {
                            bsonWriter.WriteInt64("Ticks", utcDateTime.Ticks);
                        }
                        else
                        {
                            bsonWriter.WriteUndefined("Ticks");
                        }
                        bsonWriter.WriteEndDocument();
                        break;
                    case BsonType.Int64:
                        if (bsonDateTime.IsValidDateTime)
                        {
                            bsonWriter.WriteInt64(utcDateTime.Ticks);
                        }
                        else
                        {
                            throw new BsonSerializationException("BsonDateTime is not a valid DateTime.");
                        }
                        break;
                    case BsonType.String:
                        if (dateTimeSerializationOptions.DateOnly)
                        {
                            bsonWriter.WriteString(bsonDateTime.ToUniversalTime().ToString("yyyy-MM-dd"));
                        }
                        else
                        {
                            // we're not using XmlConvert.ToString because of bugs in Mono
                            var dateTime = bsonDateTime.ToUniversalTime();
                            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
                            {
                                // serialize MinValue and MaxValue as Unspecified so we do NOT get the time zone offset
                                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                            }
                            else if (dateTime.Kind == DateTimeKind.Unspecified)
                            {
                                // serialize Unspecified as Local se we get the time zone offset
                                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                            }
                            bsonWriter.WriteString(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK"));
                        }
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid DateTime representation.", dateTimeSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
    }
}
