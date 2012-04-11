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
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Booleans.
    /// </summary>
    public class BooleanSerializer : BsonBaseSerializer
    {
        // private static fields
        private static BooleanSerializer __instance = new BooleanSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BooleanSerializer class.
        /// </summary>
        public BooleanSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Boolean))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BooleanSerializer class.
        /// </summary>
        public static BooleanSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(bool));

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Boolean:
                    return bsonReader.ReadBoolean();
                case BsonType.Double:
                    return bsonReader.ReadDouble() != 0.0;
                case BsonType.Int32:
                    return bsonReader.ReadInt32() != 0;
                case BsonType.Int64:
                    return bsonReader.ReadInt64() != 0;
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return false;
                case BsonType.String:
                    return XmlConvert.ToBoolean(bsonReader.ReadString().ToLower());
                default:
                    var message = string.Format("Cannot deserialize Boolean from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
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
            var boolValue = (bool)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Boolean:
                    bsonWriter.WriteBoolean(boolValue);
                    break;
                case BsonType.Double:
                    bsonWriter.WriteDouble(boolValue ? 1.0 : 0.0);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(boolValue ? 1 : 0);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(boolValue ? 1 : 0);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(boolValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Boolean representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for DateTimes.
    /// </summary>
    public class DateTimeSerializer : BsonBaseSerializer
    {
        // private static fields
        private static DateTimeSerializer __instance = new DateTimeSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the DateTimeSerializer class.
        /// </summary>
        public DateTimeSerializer()
            : base(DateTimeSerializationOptions.Defaults)
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the DateTimeSerializer class.
        /// </summary>
        public static DateTimeSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(DateTime));
            var dateTimeSerializationOptions = EnsureSerializationOptions<DateTimeSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            DateTime value;
            switch (bsonType)
            {
                case BsonType.DateTime:
                    // use an intermediate BsonDateTime so MinValue and MaxValue are handled correctly
                    value = BsonDateTime.Create(bsonReader.ReadDateTime()).Value;
                    break;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadDateTime("DateTime"); // ignore value (use Ticks instead)
                    value = new DateTime(bsonReader.ReadInt64("Ticks"), DateTimeKind.Utc);
                    bsonReader.ReadEndDocument();
                    break;
                case BsonType.Int64:
                    value = DateTime.SpecifyKind(new DateTime(bsonReader.ReadInt64()), DateTimeKind.Utc);
                    break;
                case BsonType.String:
                    // note: we're not using XmlConvert because of bugs in Mono
                    if (dateTimeSerializationOptions.DateOnly)
                    {
                        value = DateTime.SpecifyKind(DateTime.ParseExact(bsonReader.ReadString(), "yyyy-MM-dd", null), DateTimeKind.Utc);
                    }
                    else
                    {
                        var formats = new string[] { "yyyy-MM-ddK", "yyyy-MM-ddTHH:mm:ssK", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK" };
                        value = DateTime.ParseExact(bsonReader.ReadString(), formats, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                    }
                    break;
                default:
                    var message = string.Format("Cannot deserialize DateTime from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }

            if (dateTimeSerializationOptions.DateOnly)
            {
                if (value.TimeOfDay != TimeSpan.Zero)
                {
                    throw new FileFormatException("TimeOfDay component for DateOnly DateTime value is not zero.");
                }
                value = DateTime.SpecifyKind(value, dateTimeSerializationOptions.Kind); // not ToLocalTime or ToUniversalTime!
            }
            else
            {
                switch (dateTimeSerializationOptions.Kind)
                {
                    case DateTimeKind.Local:
                    case DateTimeKind.Unspecified:
                        value = DateTime.SpecifyKind(BsonUtils.ToLocalTime(value), dateTimeSerializationOptions.Kind);
                        break;
                    case DateTimeKind.Utc:
                        value = BsonUtils.ToUniversalTime(value);
                        break;
                }
            }

            return value;
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
            var dateTime = (DateTime)value;
            var dateTimeSerializationOptions = EnsureSerializationOptions<DateTimeSerializationOptions>(options);

            DateTime utcDateTime;
            if (dateTimeSerializationOptions.DateOnly)
            {
                if (dateTime.TimeOfDay != TimeSpan.Zero)
                {
                    throw new BsonSerializationException("TimeOfDay component is not zero.");
                }
                utcDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc); // not ToLocalTime
            }
            else
            {
                utcDateTime = BsonUtils.ToUniversalTime(dateTime);
            }
            var millisecondsSinceEpoch = BsonUtils.ToMillisecondsSinceEpoch(utcDateTime);

            switch (dateTimeSerializationOptions.Representation)
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
                    if (dateTimeSerializationOptions.DateOnly)
                    {
                        bsonWriter.WriteString(dateTime.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        // we're not using XmlConvert.ToString because of bugs in Mono
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

    /// <summary>
    /// Represents a serializer for Doubles.
    /// </summary>
    public class DoubleSerializer : BsonBaseSerializer
    {
        // private static fields
        private static DoubleSerializer __instance = new DoubleSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the DoubleSerializer class.
        /// </summary>
        public DoubleSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Double))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the DoubleSerializer class.
        /// </summary>
        public static DoubleSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(double));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return bsonReader.ReadDouble();
                case BsonType.Int32:
                    return representationSerializationOptions.ToDouble(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationSerializationOptions.ToDouble(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToDouble(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Double from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
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
            var doubleValue = (double)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(doubleValue);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(doubleValue));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(doubleValue));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(doubleValue.ToString("R", NumberFormatInfo.InvariantInfo));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Double representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Guids.
    /// </summary>
    public class GuidSerializer : BsonBaseSerializer
    {
        // private static fields
        private static GuidSerializer __instance = new GuidSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the GuidSerializer class.
        /// </summary>
        public GuidSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Binary))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the GuidSerializer class.
        /// </summary>
        public static GuidSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(Guid));

            var bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    GuidRepresentation guidRepresentation;
                    bsonReader.ReadBinaryData(out bytes, out subType, out guidRepresentation);
                    if (bytes.Length != 16)
                    {
                        message = string.Format("Expected length to be 16, not {0}.", bytes.Length);
                        throw new FileFormatException(message);
                    }
                    if (subType != BsonBinarySubType.UuidStandard && subType != BsonBinarySubType.UuidLegacy)
                    {
                        message = string.Format("Expected binary sub type to be UuidStandard or UuidLegacy, not {0}.", subType);
                        throw new FileFormatException(message);
                    }
                    if (guidRepresentation == GuidRepresentation.Unspecified)
                    {
                        throw new BsonSerializationException("GuidSerializer cannot deserialize a Guid when GuidRepresentation is Unspecified.");
                    }
                    return GuidConverter.FromBytes(bytes, guidRepresentation);
                case BsonType.String:
                    return new Guid(bsonReader.ReadString());
                default:
                    message = string.Format("Cannot deserialize Guid from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
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
            var guid = (Guid)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Binary:
                    var writerGuidRepresentation = bsonWriter.Settings.GuidRepresentation;
                    if (writerGuidRepresentation == GuidRepresentation.Unspecified)
                    {
                        throw new BsonSerializationException("GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified.");
                    }
                    var bytes = GuidConverter.ToBytes(guid, writerGuidRepresentation);
                    var subType = (writerGuidRepresentation == GuidRepresentation.Standard) ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
                    bsonWriter.WriteBinaryData(bytes, subType, writerGuidRepresentation);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(guid.ToString());
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Guid representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Int32.
    /// </summary>
    public class Int32Serializer : BsonBaseSerializer
    {
        // private static fields
        private static Int32Serializer __instance = new Int32Serializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the Int32Serializer class.
        /// </summary>
        public Int32Serializer()
            : base(new RepresentationSerializationOptions(BsonType.Int32))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the Int32Serializer class.
        /// </summary>
        public static Int32Serializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(int));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return representationSerializationOptions.ToInt32(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return bsonReader.ReadInt32();
                case BsonType.Int64:
                    return representationSerializationOptions.ToInt32(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToInt32(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Int32 from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
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
            var int32Value = (int)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(int32Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(int32Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(int32Value));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(int32Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Int32 representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Int64s.
    /// </summary>
    public class Int64Serializer : BsonBaseSerializer
    {
        // private static fields
        private static Int64Serializer __instance = new Int64Serializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the Int64Serializer class.
        /// </summary>
        public Int64Serializer()
            : base(new RepresentationSerializationOptions(BsonType.Int64))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the Int64Serializer class.
        /// </summary>
        public static Int64Serializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(long));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return representationSerializationOptions.ToInt64(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationSerializationOptions.ToInt64(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return bsonReader.ReadInt64();
                case BsonType.String:
                    return XmlConvert.ToInt64(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Int64 from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
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
            var int64Value = (long)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(int64Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(int64Value));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(int64Value);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(int64Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Int64 representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for ObjectIds.
    /// </summary>
    public class ObjectIdSerializer : BsonBaseSerializer
    {
        // private static fields
        private static ObjectIdSerializer __instance = new ObjectIdSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the ObjectIdSerializer class.
        /// </summary>
        public ObjectIdSerializer()
            : base(new RepresentationSerializationOptions(BsonType.ObjectId))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the ObjectIdSerializer class.
        /// </summary>
        public static ObjectIdSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(ObjectId));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.ObjectId:
                    int timestamp;
                    int machine;
                    short pid;
                    int increment;
                    bsonReader.ReadObjectId(out timestamp, out machine, out pid, out increment);
                    return new ObjectId(timestamp, machine, pid, increment);
                case BsonType.String:
                    return ObjectId.Parse(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize ObjectId from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
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
            var objectId = (ObjectId)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.ObjectId:
                    bsonWriter.WriteObjectId(objectId.Timestamp, objectId.Machine, objectId.Pid, objectId.Increment);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(objectId.ToString());
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid ObjectId representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Strings.
    /// </summary>
    public class StringSerializer : BsonBaseSerializer
    {
        // private static fields
        private static StringSerializer __instance = new StringSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the StringSerializer class.
        /// </summary>
        public StringSerializer()
            : base(new RepresentationSerializationOptions(BsonType.String))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the StringSerializer class.
        /// </summary>
        public static StringSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(string));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                switch (bsonType)
                {
                    case BsonType.ObjectId:
                        if (representationSerializationOptions.Representation == BsonType.ObjectId)
                        {
                            int timestamp, machine, increment;
                            short pid;
                            bsonReader.ReadObjectId(out timestamp, out machine, out pid, out increment);
                            var objectId = new ObjectId(timestamp, machine, pid, increment);
                            return objectId.ToString();
                        }
                        else
                        {
                            goto default;
                        }
                    case BsonType.String:
                        return bsonReader.ReadString();
                    case BsonType.Symbol:
                        return bsonReader.ReadSymbol();
                    default:
                        var message = string.Format("Cannot deserialize string from BsonType {0}.", bsonType);
                        throw new FileFormatException(message);
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
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var stringValue = (string)value;
                var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

                switch (representationSerializationOptions.Representation)
                {
                    case BsonType.ObjectId:
                        var id = ObjectId.Parse(stringValue);
                        bsonWriter.WriteObjectId(id.Timestamp, id.Machine, id.Pid, id.Increment);
                        break;
                    case BsonType.String:
                        bsonWriter.WriteString(stringValue);
                        break;
                    case BsonType.Symbol:
                        bsonWriter.WriteSymbol(stringValue);
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid String representation.", representationSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
    }
}
