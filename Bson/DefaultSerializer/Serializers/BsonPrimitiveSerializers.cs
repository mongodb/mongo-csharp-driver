/* Copyright 2010-2011 10gen Inc.
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
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class BooleanSerializer : BsonBaseSerializer {
        #region private static fields
        private static BooleanSerializer instance = new BooleanSerializer();
        #endregion

        #region constructors
        public BooleanSerializer() {
        }
        #endregion

        #region public static properties
        public static BooleanSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(bool), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
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
                    var message = string.Format("Cannot deserialize Boolean from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var boolValue = (bool) value;
            var representation = (options == null) ? BsonType.Boolean : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
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
                    var message = string.Format("'{0}' is not a valid representation for type 'Boolean'", representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class DateTimeSerializer : BsonBaseSerializer {
        #region private static fields
        private static DateTimeSerializer instance = new DateTimeSerializer();
        #endregion

        #region constructors
        public DateTimeSerializer() {
        }
        #endregion

        #region public static properties
        public static DateTimeSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(DateTime), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            DateTime value;

            var bsonType = bsonReader.CurrentBsonType;
            var dateTimeOptions = (options == null) ? DateTimeSerializationOptions.Defaults : (DateTimeSerializationOptions) options;
            switch (bsonType) {
                case BsonType.DateTime:
                    value = bsonReader.ReadDateTime();
                    break;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadDateTime("DateTime"); // ignore value (use Ticks instead)
                    value = DateTime.SpecifyKind(new DateTime(bsonReader.ReadInt64("Ticks")), DateTimeKind.Utc);
                    bsonReader.ReadEndDocument();
                    break;
                case BsonType.Int64:
                    value = DateTime.SpecifyKind(new DateTime(bsonReader.ReadInt64()), DateTimeKind.Utc);
                    break;
                case BsonType.String:
                    if (dateTimeOptions.DateOnly) {
                        value = DateTime.SpecifyKind(XmlConvert.ToDateTime(bsonReader.ReadString(), XmlDateTimeSerializationMode.RoundtripKind), DateTimeKind.Utc);
                    } else {
                        value = XmlConvert.ToDateTime(bsonReader.ReadString(), XmlDateTimeSerializationMode.RoundtripKind);
                    }
                    break;
                default:
                    var message = string.Format("Cannot deserialize DateTime from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }

            if (dateTimeOptions.DateOnly) {
                if (value.TimeOfDay != TimeSpan.Zero) {
                    throw new FileFormatException("TimeOfDay component for DateOnly DateTime value is not zero");
                }
                value = DateTime.SpecifyKind(value, dateTimeOptions.Kind); // not ToLocalTime or ToUniversalTime!
            } else {
                switch (dateTimeOptions.Kind) {
                    case DateTimeKind.Local:
                    case DateTimeKind.Unspecified:
                        value = ToLocalTimeHelper(value, dateTimeOptions.Kind);
                        break;
                    case DateTimeKind.Utc:
                        value = ToUniversalTimeHelper(value);
                        break;
                }
            }

            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var dateTime = (DateTime) value;
            var dateTimeOptions = (options == null) ? DateTimeSerializationOptions.Defaults : (DateTimeSerializationOptions) options;

            if (dateTimeOptions.DateOnly) {
                if (dateTime.TimeOfDay != TimeSpan.Zero) {
                    throw new BsonSerializationException("TimeOfDay component for DateOnly DateTime value is not zero");
                }
            }
            if (dateTime.Kind != DateTimeKind.Utc && dateTimeOptions.Representation != BsonType.String) {
                if (dateTimeOptions.DateOnly) {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc); // not ToUniversalTime!
                } else {
                    dateTime = ToUniversalTimeHelper(dateTime);
                }
            }

            switch (dateTimeOptions.Representation) {
                case BsonType.DateTime:
                    bsonWriter.WriteDateTime(dateTime);
                    break;
                case BsonType.Document:
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteDateTime("DateTime", dateTime);
                    bsonWriter.WriteInt64("Ticks", dateTime.Ticks);
                    bsonWriter.WriteEndDocument();
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(dateTime.Ticks);
                    break;
                case BsonType.String:
                    if (dateTimeOptions.DateOnly) {
                        bsonWriter.WriteString(dateTime.ToString("yyyy-MM-dd"));
                    } else {
                        if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) {
                            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        }
                        bsonWriter.WriteString(XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.RoundtripKind));
                    }
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'DateTime'", dateTimeOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion

        #region private methods
        private DateTime ToLocalTimeHelper(
            DateTime value,
            DateTimeKind kind
        ) {
            if (value != DateTime.MinValue && value != DateTime.MaxValue) {
                value = value.ToLocalTime();
            }
            return value = DateTime.SpecifyKind(value, kind);
        }

        private DateTime ToUniversalTimeHelper(
            DateTime value
        ) {
            if (value != DateTime.MinValue && value != DateTime.MaxValue) {
                value = value.ToUniversalTime();
            }
            return value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        #endregion
    }

    public class DoubleSerializer : BsonBaseSerializer {
        #region private static fields
        private static DoubleSerializer instance = new DoubleSerializer();
        private static RepresentationSerializationOptions defaultRepresentationOptions = new RepresentationSerializationOptions(BsonType.Double);
        #endregion

        #region constructors
        public DoubleSerializer() {
        }
        #endregion

        #region public static properties
        public static DoubleSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(double), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var representationOptions = (RepresentationSerializationOptions) options ?? defaultRepresentationOptions;
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Double:
                    return bsonReader.ReadDouble();
                case BsonType.Int32:
                    return representationOptions.ToDouble(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationOptions.ToDouble(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToDouble(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Double from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var doubleValue = (double) value;
            var representationOptions = (RepresentationSerializationOptions) options ?? defaultRepresentationOptions;
            switch (representationOptions.Representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(doubleValue);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationOptions.ToInt32(doubleValue));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationOptions.ToInt64(doubleValue));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(doubleValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Double'", representationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class GuidSerializer : BsonBaseSerializer {
        #region private static fields
        private static GuidSerializer instance = new GuidSerializer();
        #endregion

        #region constructors
        public GuidSerializer() {
        }
        #endregion

        #region public static properties
        public static GuidSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Guid), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (bytes.Length != 16) {
                        throw new FileFormatException("BinaryData length is not 16");
                    }
                    if (subType != BsonBinarySubType.Uuid) {
                        throw new FileFormatException("BinaryData sub type is not Uuid");
                    }
                    return new Guid(bytes);
                case BsonType.String:
                    return new Guid(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Guid from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var guid = (Guid) value;
            var representation = (options == null) ? BsonType.Binary : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Binary:
                    bsonWriter.WriteBinaryData(guid.ToByteArray(), BsonBinarySubType.Uuid);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(guid.ToString());
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Guid'", representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class Int32Serializer : BsonBaseSerializer {
        #region private static fields
        private static Int32Serializer instance = new Int32Serializer();
        private static RepresentationSerializationOptions defaultRepresentationOptions = new RepresentationSerializationOptions(BsonType.Int32);
        #endregion

        #region constructors
        public Int32Serializer() {
        }
        #endregion

        #region public static properties
        public static Int32Serializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(int), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var representationOptions = (RepresentationSerializationOptions) options ?? defaultRepresentationOptions;
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Double:
                    return representationOptions.ToInt32(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return bsonReader.ReadInt32();
                case BsonType.Int64:
                    return representationOptions.ToInt32(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToInt32(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Int32 from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var int32Value = (int) value;
            var representationOptions = (RepresentationSerializationOptions) options ?? defaultRepresentationOptions;
            switch (representationOptions.Representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationOptions.ToDouble(int32Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(int32Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationOptions.ToInt64(int32Value));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(int32Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Int32'", representationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class Int64Serializer : BsonBaseSerializer {
        #region private static fields
        private static Int64Serializer instance = new Int64Serializer();
        private static RepresentationSerializationOptions defaultRepresentationOptions = new RepresentationSerializationOptions(BsonType.Int64);
        #endregion

        #region constructors
        public Int64Serializer() {
        }
        #endregion

        #region public static properties
        public static Int64Serializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(long), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var representationOptions = (RepresentationSerializationOptions) options ?? defaultRepresentationOptions;
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Double:
                    return representationOptions.ToInt64(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationOptions.ToInt64(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return bsonReader.ReadInt64();
                case BsonType.String:
                    return XmlConvert.ToInt64(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Int64 from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var int64Value = (long) value;
            var representationOptions = (RepresentationSerializationOptions) options ?? defaultRepresentationOptions;
            switch (representationOptions.Representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationOptions.ToDouble(int64Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationOptions.ToInt32(int64Value));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(int64Value);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(int64Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Int64'", representationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class ObjectIdSerializer : BsonBaseSerializer {
        #region private static fields
        private static ObjectIdSerializer instance = new ObjectIdSerializer();
        #endregion

        #region constructors
        public ObjectIdSerializer() {
        }
        #endregion

        #region public static properties
        public static ObjectIdSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ObjectId), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
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
                    var message = string.Format("Cannot deserialize ObjectId from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var objectId = (ObjectId) value;
            var representation = (options == null) ? BsonType.ObjectId : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.ObjectId:
                    bsonWriter.WriteObjectId(objectId.Timestamp, objectId.Machine, objectId.Pid, objectId.Increment);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(objectId.ToString());
                    break;
                default:
                    throw new BsonInternalException("Unexpected representation");
            }
        }
        #endregion
    }

    public class StringSerializer : BsonBaseSerializer {
        #region private static fields
        private static StringSerializer instance = new StringSerializer();
        #endregion

        #region constructors
        public StringSerializer() {
        }
        #endregion

        #region public static properties
        public static StringSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(string), instance);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return bsonReader.ReadString();
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteString((string) value);
            }
        }
        #endregion
    }
}
