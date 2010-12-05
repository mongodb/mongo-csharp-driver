﻿/* Copyright 2010 10gen Inc.
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
        private static BooleanSerializer singleton = new BooleanSerializer();
        #endregion

        #region constructors
        private BooleanSerializer() {
        }
        #endregion

        #region public static properties
        public static BooleanSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(bool), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            return bsonReader.ReadBoolean();
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteBoolean((bool) value);
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
        private static DateTimeSerializer stringDateOnly = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.String, DateOnly = true });
        private static DateTimeSerializer stringRepresentation = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.String });
        private static DateTimeSerializer unspecified = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Unspecified });
        private static DateTimeSerializer unspecifiedDateOnly = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Unspecified, DateOnly = true });
        private static DateTimeSerializer utc = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Utc });
        private static DateTimeSerializer utcDateOnly = new DateTimeSerializer(new DateTimeSerializationOptions { Representation = BsonType.DateTime, Kind = DateTimeKind.Utc, DateOnly = true });
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

        public static DateTimeSerializer Utc {
            get { return utc; }
        }

        public static DateTimeSerializer UtcDateOnly {
            get { return utcDateOnly; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(DateTime), utc); // default options
            BsonSerializer.RegisterSerializer(typeof(DateTime), local.options, local);
            BsonSerializer.RegisterSerializer(typeof(DateTime), localDateOnly.options, localDateOnly);
            BsonSerializer.RegisterSerializer(typeof(DateTime), stringDateOnly.options, stringDateOnly);
            BsonSerializer.RegisterSerializer(typeof(DateTime), stringRepresentation.options, stringRepresentation);
            BsonSerializer.RegisterSerializer(typeof(DateTime), unspecified.options, unspecified);
            BsonSerializer.RegisterSerializer(typeof(DateTime), unspecifiedDateOnly.options, unspecifiedDateOnly);
            BsonSerializer.RegisterSerializer(typeof(DateTime), utc.options, utc);
            BsonSerializer.RegisterSerializer(typeof(DateTime), utcDateOnly.options, utcDateOnly);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            DateTime value;
            switch (bsonType) {
                case BsonType.DateTime:
                    value = bsonReader.ReadDateTime();
                    break;
                case BsonType.String:
                    if (options.DateOnly) {
                        value = DateTime.SpecifyKind(DateTime.Parse(bsonReader.ReadString()), DateTimeKind.Utc);
                    } else {
                        value = XmlConvert.ToDateTime(bsonReader.ReadString(), XmlDateTimeSerializationMode.RoundtripKind);
                    }
                    break;
                default:
                    var message = string.Format("Can't deserialize DateTime from BsonType: {0}", bsonType);
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

            switch (options.Representation) {
                case BsonType.DateTime:
                    if (dateTime.Kind != DateTimeKind.Utc) {
                        if (options.DateOnly) {
                            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc); // not ToUniversalTime!
                        } else {
                            dateTime = ToUniversalTimeHelper(dateTime);
                        }
                    }
                    bsonWriter.WriteDateTime(dateTime);
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
                    var message = string.Format("Invalid representation for DateTime: {0}", options.Representation);
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
