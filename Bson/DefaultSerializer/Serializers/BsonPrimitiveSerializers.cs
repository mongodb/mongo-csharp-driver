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
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadBoolean(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            bsonWriter.WriteBoolean(name, (bool) value);
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
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            DateTime value;
            switch (bsonType) {
                case BsonType.DateTime:
                    value = bsonReader.ReadDateTime(out name);
                    break;
                case BsonType.String:
                    if (options.DateOnly) {
                        value = DateTime.SpecifyKind(DateTime.Parse(bsonReader.ReadString(out name)), DateTimeKind.Utc);
                    } else {
                        value = XmlConvert.ToDateTime(bsonReader.ReadString(out name), XmlDateTimeSerializationMode.RoundtripKind);
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

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            var dateTimeValue = (DateTime) value;
            if (options.DateOnly) {
                if (dateTimeValue.TimeOfDay != TimeSpan.Zero) {
                    throw new BsonSerializationException("TimeOfDay component for DateOnly DateTime value is not zero");
                }
            }

            switch (options.Representation) {
                case BsonType.DateTime:
                    if (dateTimeValue.Kind != DateTimeKind.Utc) {
                        if (options.DateOnly) {
                            dateTimeValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc); // not ToUniversalTime!
                        } else {
                            dateTimeValue = ToUniversalTimeHelper(dateTimeValue);
                        }
                    }
                    bsonWriter.WriteDateTime(name, dateTimeValue);
                    break;
                case BsonType.String:
                    if (options.DateOnly) {
                        bsonWriter.WriteString(name, dateTimeValue.ToString("yyyy-MM-dd"));
                    } else {
                        if (dateTimeValue == DateTime.MinValue || dateTimeValue == DateTime.MaxValue) {
                            dateTimeValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc);
                        }
                        bsonWriter.WriteString(name, XmlConvert.ToString(dateTimeValue, XmlDateTimeSerializationMode.RoundtripKind));
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
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadDouble(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            bsonWriter.WriteDouble(name, (double) value);
        }
        #endregion
    }

    public class GuidSerializer : BsonBaseSerializer {
        #region private static fields
        private static GuidSerializer singleton = new GuidSerializer();
        #endregion

        #region constructors
        private GuidSerializer() {
        }
        #endregion

        #region public static properties
        public static GuidSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Guid), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            byte[] bytes;
            BsonBinarySubType subType;
            bsonReader.ReadBinaryData(out name, out bytes, out subType);
            if (bytes.Length != 16) {
                throw new FileFormatException("BinaryData length is not 16");
            }
            if (subType != BsonBinarySubType.Uuid) {
                throw new FileFormatException("BinaryData sub type is not Uuid");
            }
            return new Guid(bytes);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (Guid) obj;
            bsonWriter.WriteBinaryData(name, value.ToByteArray(), BsonBinarySubType.Uuid);
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
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadInt32(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            bsonWriter.WriteInt32(name, (int) value);
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
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadInt64(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            bsonWriter.WriteInt64(name, (long) value);
        }
        #endregion
    }

    public class ObjectIdSerializer : BsonBaseSerializer {
        #region private static fields
        private static ObjectIdSerializer singleton = new ObjectIdSerializer();
        #endregion

        #region constructors
        private ObjectIdSerializer() {
        }
        #endregion

        #region public static properties
        public static ObjectIdSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ObjectId), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            int timestamp;
            long machinePidIncrement;
            bsonReader.ReadObjectId(out name, out timestamp, out machinePidIncrement);
            return new ObjectId(timestamp, machinePidIncrement);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (ObjectId) obj;
            bsonWriter.WriteObjectId(name, value.Timestamp, value.MachinePidIncrement);
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
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else {
                return bsonReader.ReadString(out name);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteString(name, (string) value);
            }
        }
        #endregion
    }
}
