/* Copyright 2010 10gen Inc.
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
        private static BooleanSerializer booleanRepresentation = new BooleanSerializer(BsonType.Boolean);
        private static BooleanSerializer doubleRepresentation = new BooleanSerializer(BsonType.Double);
        private static BooleanSerializer int32Representation = new BooleanSerializer(BsonType.Int32);
        private static BooleanSerializer int64Representation = new BooleanSerializer(BsonType.Int64);
        private static BooleanSerializer stringRepresentation = new BooleanSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private BooleanSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static BooleanSerializer BooleanRepresentation {
            get { return booleanRepresentation; }
        }

        public static BooleanSerializer DoubleRepresentation {
            get { return doubleRepresentation; }
        }

        public static BooleanSerializer Int32Representation {
            get { return int32Representation; }
        }

        public static BooleanSerializer Int64Representation {
            get { return int64Representation; }
        }

        public static BooleanSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(bool), null, booleanRepresentation); // default representation
            BsonSerializer.RegisterSerializer(typeof(bool), BsonType.Boolean, booleanRepresentation);
            BsonSerializer.RegisterSerializer(typeof(bool), BsonType.Double, doubleRepresentation);
            BsonSerializer.RegisterSerializer(typeof(bool), BsonType.Int32, int32Representation);
            BsonSerializer.RegisterSerializer(typeof(bool), BsonType.Int64, int64Representation);
            BsonSerializer.RegisterSerializer(typeof(bool), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            switch (bsonReader.CurrentBsonType) {
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
                    var message = string.Format("Cannot deserialize Boolean from BsonType: {0}", bsonReader.CurrentBsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            var boolValue = (bool) value;
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

    public class DateTimeSerializationOptions {
        #region private fields
        private bool dateOnly = false;
        private DateTimeKind kind = DateTimeKind.Utc;
        private BsonType representation = BsonType.DateTime;
        #endregion

        #region public properties
        public bool DateOnly {
            get { return dateOnly; }
            set { dateOnly = value; }
        }

        public DateTimeKind Kind {
            get { return kind; }
            set { kind = value; }
        }

        public BsonType Representation {
            get { return representation; }
            set { representation = value; }
        }
        #endregion

        #region public methods
        public override bool Equals(
            object obj
        ) {
            if (obj == null || obj.GetType() != typeof(DateTimeSerializationOptions)) {
                return false;
            }
            var other = (DateTimeSerializationOptions) obj;
            return
                this.dateOnly == other.dateOnly &&
                this.kind == other.kind &&
                this.representation == other.representation;
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + dateOnly.GetHashCode();
            hash = 37 * hash + kind.GetHashCode();
            hash = 37 * hash + representation.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return string.Format("DateOnly={0}, Kind={1}, Representation={2}", dateOnly, kind, representation);
        }
        #endregion
    }

    public class DateTimeSerializer : BsonBaseSerializer {
        #region private static fields
        private static DateTimeSerializer local = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Local });
        private static DateTimeSerializer localDateOnly = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Local, DateOnly = true });
        private static DateTimeSerializer localTicks = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.Int64, Kind = DateTimeKind.Local });
        private static DateTimeSerializer stringDateOnly = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.String, DateOnly = true });
        private static DateTimeSerializer stringRepresentation = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.String });
        private static DateTimeSerializer unspecified = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Unspecified });
        private static DateTimeSerializer unspecifiedDateOnly = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Unspecified, DateOnly = true });
        private static DateTimeSerializer unspecifiedTicks = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.Int64, Kind = DateTimeKind.Unspecified });
        private static DateTimeSerializer utc = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Utc });
        private static DateTimeSerializer utcDateOnly = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Utc, DateOnly = true });
        private static DateTimeSerializer utcTicks = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.Int64, Kind = DateTimeKind.Utc });
        #endregion

        #region private fields
        private DateTimeSerializationOptions options;
        #endregion

        #region constructors
        private DateTimeSerializer(
            DateTimeSerializationOptions options
        ) {
            this.options = options;
        }
        #endregion

        #region public static properties
        public static DateTimeSerializer Local {
            get { return local; }
        }

        public static DateTimeSerializer LocalDateOnly {
            get { return localDateOnly; }
        }

        public static DateTimeSerializer LocalTicks {
            get { return localTicks; }
        }

        public static DateTimeSerializer StringDateOnly {
            get { return stringDateOnly; }
        }

        public static DateTimeSerializer StringRepresentation {
            get { return stringRepresentation; }
        }

        public static DateTimeSerializer Unspecified {
            get { return unspecified; }
        }

        public static DateTimeSerializer UnspecifiedDateOnly {
            get { return unspecifiedDateOnly; }
        }

        public static DateTimeSerializer UnspecifiedTicks {
            get { return unspecifiedTicks; }
        }

        public static DateTimeSerializer Utc {
            get { return utc; }
        }

        public static DateTimeSerializer UtcDateOnly {
            get { return utcDateOnly; }
        }

        public static DateTimeSerializer UtcTicks {
            get { return utcTicks; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(DateTime), utc); // default options
            BsonSerializer.RegisterSerializer(typeof(DateTime), local.options, local);
            BsonSerializer.RegisterSerializer(typeof(DateTime), localDateOnly.options, localDateOnly);
            BsonSerializer.RegisterSerializer(typeof(DateTime), localTicks.options, localTicks);
            BsonSerializer.RegisterSerializer(typeof(DateTime), stringDateOnly.options, stringDateOnly);
            BsonSerializer.RegisterSerializer(typeof(DateTime), stringRepresentation.options, stringRepresentation);
            BsonSerializer.RegisterSerializer(typeof(DateTime), unspecified.options, unspecified);
            BsonSerializer.RegisterSerializer(typeof(DateTime), unspecifiedDateOnly.options, unspecifiedDateOnly);
            BsonSerializer.RegisterSerializer(typeof(DateTime), unspecifiedTicks.options, unspecifiedTicks);
            BsonSerializer.RegisterSerializer(typeof(DateTime), utc.options, utc);
            BsonSerializer.RegisterSerializer(typeof(DateTime), utcDateOnly.options, utcDateOnly);
            BsonSerializer.RegisterSerializer(typeof(DateTime), utcTicks.options, utcTicks);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            DateTime value;
            switch (bsonReader.CurrentBsonType) {
                case BsonType.DateTime:
                    value = bsonReader.ReadDateTime();
                    break;
                case BsonType.Int64:
                    value = DateTime.SpecifyKind(new DateTime(bsonReader.ReadInt64()), DateTimeKind.Utc);
                    break;
                case BsonType.String:
                    if (options.DateOnly) {
                        value = DateTime.SpecifyKind(DateTime.Parse(bsonReader.ReadString()), DateTimeKind.Utc);
                    } else {
                        value = XmlConvert.ToDateTime(bsonReader.ReadString(), XmlDateTimeSerializationMode.RoundtripKind);
                    }
                    break;
                default:
                    var message = string.Format("Cannot deserialize DateTime from BsonType: {0}", bsonReader.CurrentBsonType);
                    throw new FileFormatException(message);
            }

            if (options.DateOnly) {
                if (value.TimeOfDay != TimeSpan.Zero) {
                    throw new FileFormatException("TimeOfDay component for DateOnly DateTime value is not zero");
                }
                value = DateTime.SpecifyKind(value, options.Kind); // not ToLocalTime or ToUniversalTime!
            } else {
                switch (options.Kind) {
                    case DateTimeKind.Local:
                    case DateTimeKind.Unspecified:
                        value = ToLocalTimeHelper(value, options.Kind);
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
            bool serializeIdFirst
        ) {
            var dateTime = (DateTime) value;
            if (options.DateOnly) {
                if (dateTime.TimeOfDay != TimeSpan.Zero) {
                    throw new BsonSerializationException("TimeOfDay component for DateOnly DateTime value is not zero");
                }
            }
            if (dateTime.Kind != DateTimeKind.Utc && options.Representation != BsonType.String) {
                if (options.DateOnly) {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc); // not ToUniversalTime!
                } else {
                    dateTime = ToUniversalTimeHelper(dateTime);
                }
            }

            switch (options.Representation) {
                case BsonType.DateTime:
                    bsonWriter.WriteDateTime(dateTime);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(dateTime.Ticks);
                    break;
                case BsonType.String:
                    if (options.DateOnly) {
                        bsonWriter.WriteString(dateTime.ToString("yyyy-MM-dd"));
                    } else {
                        if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) {
                            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        }
                        bsonWriter.WriteString(XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.RoundtripKind));
                    }
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'DateTime'", options.Representation);
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
        private static DoubleSerializer singleton = new DoubleSerializer();
        #endregion

        #region constructors
        private DoubleSerializer() {
        }
        #endregion

        #region public static properties
        public static DoubleSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(double), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            double value;
            bool lostData = false;
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Int32:
                    value = (double) bsonReader.ReadInt32();
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (double) int64Value;
                    lostData = (long) value != int64Value;
                    break;
                default:
                    value = bsonReader.ReadDouble();
                    break;
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to Double", bsonType);
                throw new FileFormatException(message);
            }
            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteDouble((double) value);
        }
        #endregion
    }

    public class GuidSerializer : BsonBaseSerializer {
        #region private static fields
        private static GuidSerializer binaryRepresentation = new GuidSerializer(BsonType.Binary);
        private static GuidSerializer stringRepresentation = new GuidSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private GuidSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static GuidSerializer BinaryRepresentation {
            get { return binaryRepresentation; }
        }

        public static GuidSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Guid), null, binaryRepresentation); // default representation
            BsonSerializer.RegisterSerializer(typeof(Guid), BsonType.Binary, binaryRepresentation);
            BsonSerializer.RegisterSerializer(typeof(Guid), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Binary) {
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
            } else if (bsonType == BsonType.String) {
                return new Guid(bsonReader.ReadString());
            } else {
                var message = string.Format("Cannot deserialize Guid from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            var guid = (Guid) value;
            switch (representation) {
                case BsonType.Binary:
                    bsonWriter.WriteBinaryData(guid.ToByteArray(), BsonBinarySubType.Uuid);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(guid.ToString());
                    break;
                default:
                    throw new BsonInternalException("Unexpected representation");
            }
        }
        #endregion
    }

    public class Int32Serializer : BsonBaseSerializer {
        #region private static fields
        private static Int32Serializer singleton = new Int32Serializer();
        #endregion

        #region constructors
        private Int32Serializer() {
        }
        #endregion

        #region public static properties
        public static Int32Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(int), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            int value;
            bool lostData = false;
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Double:
                    var doubleValue = bsonReader.ReadDouble();
                    value = (int) doubleValue;
                    lostData = (double) value != doubleValue;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (int) int64Value;
                    lostData = (long) value != int64Value;
                    break;
                default:
                    value = bsonReader.ReadInt32();
                    break;
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to Int32", bsonType);
                throw new FileFormatException(message);
            }
            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt32((int) value);
        }
        #endregion
    }

    public class Int64Serializer : BsonBaseSerializer {
        #region private static fields
        private static Int64Serializer singleton = new Int64Serializer();
        #endregion

        #region constructors
        private Int64Serializer() {
        }
        #endregion

        #region public static properties
        public static Int64Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(long), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            long value;
            bool lostData = false;
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Double:
                    var doubleValue = bsonReader.ReadDouble();
                    value = (long) doubleValue;
                    lostData = (double) value != doubleValue;
                    break;
                case BsonType.Int32:
                    value = bsonReader.ReadInt32();
                    break;
                default:
                    value = bsonReader.ReadInt64();
                    break;
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to Int64", bsonType);
                throw new FileFormatException(message);
            }
            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt64((long) value);
        }
        #endregion
    }

    public class ObjectIdSerializer : BsonBaseSerializer {
        #region private static fields
        private static ObjectIdSerializer objectIdRepresentation = new ObjectIdSerializer(BsonType.ObjectId);
        private static ObjectIdSerializer stringRepresentation = new ObjectIdSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private ObjectIdSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static ObjectIdSerializer ObjectIdRepresentation {
            get { return objectIdRepresentation; }
        }

        public static ObjectIdSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ObjectId), null, objectIdRepresentation); // default representation
            BsonSerializer.RegisterSerializer(typeof(ObjectId), BsonType.ObjectId, objectIdRepresentation);
            BsonSerializer.RegisterSerializer(typeof(ObjectId), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.ObjectId) {
                int timestamp;
                int machine;
                short pid;
                int increment;
                bsonReader.ReadObjectId(out timestamp, out machine, out pid, out increment);
                return new ObjectId(timestamp, machine, pid, increment);
            } else if (bsonType == BsonType.String) {
                return ObjectId.Parse(bsonReader.ReadString());
            } else {
                var message = string.Format("Cannot deserialize ObjectId from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }

        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            var objectId = (ObjectId) value;
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
        private static StringSerializer singleton = new StringSerializer();
        #endregion

        #region constructors
        private StringSerializer() {
        }
        #endregion

        #region public static properties
        public static StringSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(string), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
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
            bool serializeIdFirst
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
