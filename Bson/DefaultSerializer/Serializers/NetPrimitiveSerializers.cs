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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class ByteSerializer : BsonBaseSerializer {
        #region private static fields
        private static ByteSerializer singleton = new ByteSerializer();
        #endregion

        #region constructors
        private ByteSerializer() {
        }
        #endregion

        #region public static properties
        public static ByteSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(byte), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (byte) bsonReader.ReadInt32(out name);
            } else {
                var message = string.Format("Cannot deserialize Byte from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            bsonWriter.WriteInt32(name, (byte) obj);
        }
        #endregion
    }

    public class CharSerializer : BsonBaseSerializer {
        #region private static fields
        private static CharSerializer int32Representation = new CharSerializer(BsonType.Int32);
        private static CharSerializer stringRepresentation = new CharSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private CharSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static CharSerializer Int32Representation {
            get { return int32Representation; }
        }

        public static CharSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(char), int32Representation); // default representation
            BsonSerializer.RegisterSerializer(typeof(char), BsonType.Int32, int32Representation);
            BsonSerializer.RegisterSerializer(typeof(char), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (char) bsonReader.ReadInt32(out name);
            } else  if (bsonType == BsonType.String) {
                return (char) bsonReader.ReadString(out name)[0];
            } else {
                var message = string.Format("Cannot deserialize Char from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            switch (representation) {
                case BsonType.Int32:
                    bsonWriter.WriteInt32(name, (int) (char) obj);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(name, new string(new [] { (char) obj }));
                    break;
                default:
                    throw new BsonInternalException("Unexpected representation");
            }
        }
        #endregion
    }

    public class CultureInfoSerializer : BsonBaseSerializer {
        #region private static fields
        private static CultureInfoSerializer singleton = new CultureInfoSerializer();
        #endregion

        #region constructors
        private CultureInfoSerializer() {
        }
        #endregion

        #region public static properties
        public static CultureInfoSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(CultureInfo), singleton);
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
            } else if (bsonType == BsonType.String) {
                return new CultureInfo(bsonReader.ReadString(out name));
            } else {
                var message = string.Format("Cannot deserialize CultureInfo from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            if (obj == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteString(name, ((CultureInfo) obj).ToString());
            }
        }
        #endregion
    }

    public class DateTimeOffsetSerializer : BsonBaseSerializer {
        #region private static fields
        private static DateTimeOffsetSerializer arrayRepresentation = new DateTimeOffsetSerializer(BsonType.Array);
        private static DateTimeOffsetSerializer stringRepresentation = new DateTimeOffsetSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private DateTimeOffsetSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static DateTimeOffsetSerializer ArrayRepresentation {
            get { return arrayRepresentation; }
        }

        public static DateTimeOffsetSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(DateTimeOffset), arrayRepresentation); // default representation
            BsonSerializer.RegisterSerializer(typeof(DateTimeOffset), BsonType.Array, arrayRepresentation);
            BsonSerializer.RegisterSerializer(typeof(DateTimeOffset), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Array) {
                bsonReader.ReadArrayName(out name);
                bsonReader.ReadStartDocument();
                var dateTime = new DateTime(bsonReader.ReadInt64("0"));
                var offset = TimeSpan.FromMinutes(bsonReader.ReadInt32("1"));
                bsonReader.ReadEndDocument();
                return new DateTimeOffset(dateTime, offset);
            } else if (bsonType == BsonType.String) {
                return XmlConvert.ToDateTimeOffset(bsonReader.ReadString(out name));
            } else {
                var message = string.Format("Cannot deserialize DateTimeOffset from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC
            var value = (DateTimeOffset) obj;
            switch (representation) {
                case BsonType.Array:
                    bsonWriter.WriteArrayName(name);
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteInt64("0", value.DateTime.Ticks);
                    bsonWriter.WriteInt32("1", (int) value.Offset.TotalMinutes);
                    bsonWriter.WriteEndDocument();
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(name, XmlConvert.ToString(value));
                    break;
                default:
                    throw new BsonInternalException("Unexpected representation");
            }
        }
        #endregion
    }

    public class DecimalSerializer : BsonBaseSerializer {
        #region private static fields
        private static DecimalSerializer arrayRepresentation = new DecimalSerializer(BsonType.Array);
        private static DecimalSerializer stringRepresentation = new DecimalSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private DecimalSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static DecimalSerializer ArrayRepresentation {
            get { return arrayRepresentation; }
        }

        public static DecimalSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Decimal), stringRepresentation); // default representation
            BsonSerializer.RegisterSerializer(typeof(Decimal), BsonType.Array, arrayRepresentation);
            BsonSerializer.RegisterSerializer(typeof(Decimal), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Array) {
                var bits = new int[4];
                bsonReader.ReadArrayName(out name);
                bsonReader.ReadStartDocument();
                bits[0] = bsonReader.ReadInt32("0");
                bits[1] = bsonReader.ReadInt32("1");
                bits[2] = bsonReader.ReadInt32("2");
                bits[3] = bsonReader.ReadInt32("3");
                bsonReader.ReadEndDocument();
                return new decimal(bits);
            } else if (bsonType == BsonType.String) {
                return XmlConvert.ToDecimal(bsonReader.ReadString(out name));
            } else {
                var message = string.Format("Cannot deserialize Decimal from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (Decimal) obj;
            switch (representation) {
                case BsonType.Array:
                    var bits = Decimal.GetBits(value);
                    bsonWriter.WriteArrayName(name);
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteInt32("0", bits[0]);
                    bsonWriter.WriteInt32("1", bits[1]);
                    bsonWriter.WriteInt32("2", bits[2]);
                    bsonWriter.WriteInt32("3", bits[3]);
                    bsonWriter.WriteEndDocument();
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(name, XmlConvert.ToString(value));
                    break;
                default:
                    throw new BsonInternalException("Unexpected representation");
            }
        }
        #endregion
    }

    public class Int16Serializer : BsonBaseSerializer {
        #region private static fields
        private static Int16Serializer singleton = new Int16Serializer();
        #endregion

        #region constructors
        private Int16Serializer() {
        }
        #endregion

        #region public static properties
        public static Int16Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(short), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (short) bsonReader.ReadInt32(out name);
            } else {
                var message = string.Format("Cannot deserialize Int16 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            bsonWriter.WriteInt32(name, (short) obj);
        }
        #endregion
    }

    public class SByteSerializer : BsonBaseSerializer {
        #region private static fields
        private static SByteSerializer singleton = new SByteSerializer();
        #endregion

        #region constructors
        private SByteSerializer() {
        }
        #endregion

        #region public static properties
        public static SByteSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(sbyte), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (sbyte) bsonReader.ReadInt32(out name);
            } else {
                var message = string.Format("Cannot deserialize SByte from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            bsonWriter.WriteInt32(name, (sbyte) obj);
        }
        #endregion
    }

    public class SingleSerializer : BsonBaseSerializer {
        #region private static fields
        private static SingleSerializer singleton = new SingleSerializer();
        #endregion

        #region constructors
        private SingleSerializer() {
        }
        #endregion

        #region public static properties
        public static SingleSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(float), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            double doubleValue;
            if (bsonType == BsonType.Double) {
                doubleValue = bsonReader.ReadDouble(out name);
            } else {
                var message = string.Format("Cannot deserialize Single from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
            return doubleValue == double.MinValue ? float.MinValue : doubleValue == double.MaxValue ? float.MaxValue : (float) doubleValue;
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var floatValue = (float) obj;
            var doubleValue = (floatValue == float.MinValue) ? double.MinValue : (floatValue == float.MaxValue) ? double.MaxValue : floatValue;
            bsonWriter.WriteDouble(name, doubleValue);
        }
        #endregion
    }

    public class TimeSpanSerializer : BsonBaseSerializer {
        #region private static fields
        private static TimeSpanSerializer int64Representation = new TimeSpanSerializer(BsonType.Int64);
        private static TimeSpanSerializer stringRepresentation = new TimeSpanSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private TimeSpanSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static TimeSpanSerializer Int64Representation {
            get { return int64Representation; }
        }

        public static TimeSpanSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(TimeSpan), stringRepresentation); // default representation
            BsonSerializer.RegisterSerializer(typeof(TimeSpan), BsonType.Int64, int64Representation);
            BsonSerializer.RegisterSerializer(typeof(TimeSpan), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int64) {
                return new TimeSpan(bsonReader.ReadInt64(out name));
            } else if (bsonType == BsonType.String) {
                return TimeSpan.Parse(bsonReader.ReadString(out name));
            } else {
                var message = string.Format("Cannot deserialize TimeSpan from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (TimeSpan) obj;
            switch (representation) {
                case BsonType.Int64:
                    bsonWriter.WriteInt64(name, value.Ticks);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(name, value.ToString());
                    break;
                default:
                    throw new BsonInternalException("Unexpected representation");
            }
        }
        #endregion
    }

    public class UInt16Serializer : BsonBaseSerializer {
        #region private static fields
        private static UInt16Serializer singleton = new UInt16Serializer();
        #endregion

        #region constructors
        private UInt16Serializer() {
        }
        #endregion

        #region public static properties
        public static UInt16Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ushort), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (ushort) bsonReader.ReadInt32(out name);
            } else {
                var message = string.Format("Cannot deserialize UInt16 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            bsonWriter.WriteInt32(name, (ushort) obj);
        }
        #endregion
    }

    public class UInt32Serializer : BsonBaseSerializer {
        #region private static fields
        private static UInt32Serializer singleton = new UInt32Serializer();
        #endregion

        #region constructors
        private UInt32Serializer() {
        }
        #endregion

        #region public static properties
        public static UInt32Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(uint), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (uint) bsonReader.ReadInt32(out name);
            } else {
                var message = string.Format("Cannot deserialize UInt32 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            bsonWriter.WriteInt32(name, (int) (uint) obj);
        }
        #endregion
    }

    public class UInt64Serializer : BsonBaseSerializer {
        #region private static fields
        private static UInt64Serializer singleton = new UInt64Serializer();
        #endregion

        #region constructors
        private UInt64Serializer() {
        }
        #endregion

        #region public static properties
        public static UInt64Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ulong), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int64) {
                return (ulong) bsonReader.ReadInt64(out name);
            } else {
                var message = string.Format("Cannot deserialize UInt64 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            bsonWriter.WriteInt64(name, (long) (ulong) obj);
        }
        #endregion
    }
}
