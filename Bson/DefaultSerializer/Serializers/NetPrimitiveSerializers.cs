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
using System.Collections;
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
    public class BitArraySerializer : BsonBaseSerializer {
        #region private static fields
        private static BitArraySerializer singleton = new BitArraySerializer();
        #endregion

        #region constructors
        private BitArraySerializer() {
        }
        #endregion

        #region public static properties
        public static BitArraySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BitArray), singleton);
        }
        #endregion

        #region public methods
        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Binary) {
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData(out bytes, out subType);
                if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary) {
                    var message = string.Format("Invalid Binary sub type: {0}", subType);
                    throw new FileFormatException(message);
                }
                return new BitArray(bytes);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadStartDocument();
                var length = bsonReader.ReadInt32("Length");
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData("Bytes", out bytes, out subType);
                if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary) {
                    var message = string.Format("Invalid Binary sub type: {0}", subType);
                    throw new FileFormatException(message);
                }
                bsonReader.ReadEndDocument();
                var bitArray = new BitArray(bytes);
                bitArray.Length = length;
                return bitArray;
            } else {
                var message = string.Format("Cannot deserialize Byte[] from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }
        #pragma warning restore 618

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bitArray = (BitArray) value;
                if ((bitArray.Length % 8) == 0) {
                    bsonWriter.WriteBinaryData(GetBytes(bitArray), BsonBinarySubType.Binary);
                } else {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteInt32("Length", bitArray.Length);
                    bsonWriter.WriteBinaryData("Bytes", GetBytes(bitArray), BsonBinarySubType.Binary);
                    bsonWriter.WriteEndDocument();
                }
            }
        }
        #endregion

        #region private methods
        private byte[] GetBytes(
            BitArray bitArray
        ) {
            // TODO: is there a more efficient way to do this?
            var bytes = new byte[(bitArray.Length + 7) / 8];
            var i = 0;
            foreach (bool value in bitArray) {
                if (value) {
                    var index = i / 8;
                    var bit = i % 8;
                    bytes[index] |= (byte) (1 << bit);
                }
                i++;
            }
            return bytes;
        }
        #endregion
    }

    public class ByteArraySerializer : BsonBaseSerializer {
        #region private static fields
        private static ByteArraySerializer singleton = new ByteArraySerializer();
        #endregion

        #region constructors
        private ByteArraySerializer() {
        }
        #endregion

        #region public static properties
        public static ByteArraySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(byte[]), singleton);
        }
        #endregion

        #region public methods
        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Binary) {
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData(out bytes, out subType);
                if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary) {
                    var message = string.Format("Invalid Binary sub type: {0}", subType);
                    throw new FileFormatException(message);
                }
                return bytes;
            } else {
                var message = string.Format("Cannot deserialize Byte[] from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }
        #pragma warning restore 618

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteBinaryData((byte[]) value, BsonBinarySubType.Binary);
            }
        }
        #endregion
    }

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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int32) {
                return (byte) bsonReader.ReadInt32();
            } else {
                var message = string.Format("Cannot deserialize Byte from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt32((byte) value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int32) {
                return (char) bsonReader.ReadInt32();
            } else  if (bsonType == BsonType.String) {
                return (char) bsonReader.ReadString()[0];
            } else {
                var message = string.Format("Cannot deserialize Char from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            switch (representation) {
                case BsonType.Int32:
                    bsonWriter.WriteInt32((int) (char) value);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(new string(new[] { (char) value }));
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.String) {
                return new CultureInfo(bsonReader.ReadString());
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadStartDocument();
                var name = bsonReader.ReadString("Name");
                var useUserOverride = bsonReader.ReadBoolean("UseUserOverride");
                bsonReader.ReadEndDocument();
                return new CultureInfo(name, useUserOverride);
            } else {
                var message = string.Format("Cannot deserialize CultureInfo from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
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
                var cultureInfo = (CultureInfo) value;
                if (cultureInfo.UseUserOverride) {
                    // the default for UseUserOverride is true so we don't need to serialize it
                    bsonWriter.WriteString(cultureInfo.Name);
                } else {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("Name", cultureInfo.Name);
                    bsonWriter.WriteBoolean("UseUserOverride", cultureInfo.UseUserOverride);
                    bsonWriter.WriteEndDocument();
                }
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Array) {
                var array = BsonArray.ReadFrom(bsonReader);
                var dateTime = new DateTime(array[0].AsInt64);
                var offset = TimeSpan.FromMinutes(array[1].AsInt32);
                return new DateTimeOffset(dateTime, offset);
            } else if (bsonType == BsonType.String) {
                return XmlConvert.ToDateTimeOffset(bsonReader.ReadString());
            } else {
                var message = string.Format("Cannot deserialize DateTimeOffset from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC
            var dateTimeOffset = (DateTimeOffset) value;
            switch (representation) {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    bsonWriter.WriteInt64("0", dateTimeOffset.DateTime.Ticks);
                    bsonWriter.WriteInt32("1", (int) dateTimeOffset.Offset.TotalMinutes);
                    bsonWriter.WriteEndArray();
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(dateTimeOffset));
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Array) {
                var array = BsonArray.ReadFrom(bsonReader);
                var bits = new int[4];
                bits[0] = array[0].AsInt32;
                bits[1] = array[1].AsInt32;
                bits[2] = array[2].AsInt32;
                bits[3] = array[3].AsInt32;
                return new decimal(bits);
            } else if (bsonType == BsonType.String) {
                return XmlConvert.ToDecimal(bsonReader.ReadString());
            } else {
                var message = string.Format("Cannot deserialize Decimal from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            var @decimal = (Decimal) value;
            switch (representation) {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    var bits = Decimal.GetBits(@decimal);
                    bsonWriter.WriteInt32("0", bits[0]);
                    bsonWriter.WriteInt32("1", bits[1]);
                    bsonWriter.WriteInt32("2", bits[2]);
                    bsonWriter.WriteInt32("3", bits[3]);
                    bsonWriter.WriteEndArray();
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(@decimal));
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int32) {
                return (short) bsonReader.ReadInt32();
            } else {
                var message = string.Format("Cannot deserialize Int16 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt32((short) value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int32) {
                return (sbyte) bsonReader.ReadInt32();
            } else {
                var message = string.Format("Cannot deserialize SByte from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt32((sbyte) value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            double doubleValue;
            if (bsonType == BsonType.Double) {
                doubleValue = bsonReader.ReadDouble();
            } else {
                var message = string.Format("Cannot deserialize Single from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
            return doubleValue == double.MinValue ? float.MinValue : doubleValue == double.MaxValue ? float.MaxValue : (float) doubleValue;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            var floatValue = (float) value;
            var doubleValue = (floatValue == float.MinValue) ? double.MinValue : (floatValue == float.MaxValue) ? double.MaxValue : floatValue;
            bsonWriter.WriteDouble(doubleValue);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int64) {
                return new TimeSpan(bsonReader.ReadInt64());
            } else if (bsonType == BsonType.String) {
                return TimeSpan.Parse(bsonReader.ReadString());
            } else {
                var message = string.Format("Cannot deserialize TimeSpan from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            var timeSpan = (TimeSpan) value;
            switch (representation) {
                case BsonType.Int64:
                    bsonWriter.WriteInt64(timeSpan.Ticks);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(timeSpan.ToString());
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int32) {
                return (ushort) bsonReader.ReadInt32();
            } else {
                var message = string.Format("Cannot deserialize UInt16 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt32((ushort) value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int32) {
                return (uint) bsonReader.ReadInt32();
            } else {
                var message = string.Format("Cannot deserialize UInt32 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt32((int) (uint) value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int64) {
                return (ulong) bsonReader.ReadInt64();
            } else {
                var message = string.Format("Cannot deserialize UInt64 from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteInt64((long) (ulong) value);
        }
        #endregion
    }

    public class UriSerializer : BsonBaseSerializer {
        #region private static fields
        private static UriSerializer singleton = new UriSerializer();
        #endregion

        #region constructors
        private UriSerializer() {
        }
        #endregion

        #region public static properties
        public static UriSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Uri), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.String) {
                return new Uri(bsonReader.ReadString());
            } else {
                var message = string.Format("Cannot deserialize Uri from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
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
                bsonWriter.WriteString(((Uri) value).AbsoluteUri);
            }
        }
        #endregion
    }

    public class VersionSerializer : BsonBaseSerializer {
        #region private static fields
        private static VersionSerializer documentRepresentation = new VersionSerializer(BsonType.Document);
        private static VersionSerializer stringRepresentation = new VersionSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private VersionSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static VersionSerializer DocumentRepresentation {
            get { return documentRepresentation; }
        }

        public static VersionSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Version), stringRepresentation); // default representation
            BsonSerializer.RegisterSerializer(typeof(Version), BsonType.Document, documentRepresentation);
            BsonSerializer.RegisterSerializer(typeof(Version), BsonType.String, stringRepresentation);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadStartDocument();
                int major = -1, minor = -1, build = -1, revision = -1;
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var name = bsonReader.ReadName();
                    switch (name) {
                        case "Major": major = bsonReader.ReadInt32(); break;
                        case "Minor": minor = bsonReader.ReadInt32(); break;
                        case "Build": build = bsonReader.ReadInt32(); break;
                        case "Revision": revision = bsonReader.ReadInt32(); break;
                        default:
                            var message = string.Format("Unrecognized element in Version: {0}", name);
                            throw new FileFormatException(message);
                    }
                }
                bsonReader.ReadEndDocument();
                if (major == -1) {
                    var message = string.Format("Version missing Major element");
                    throw new FileFormatException(message);
                } else if (minor == -1) {
                    var message = string.Format("Version missing Minor element");
                    throw new FileFormatException(message);
                } else if (build == -1) {
                    return new Version(major, minor);
                } else if (revision == -1) {
                    return new Version(major, minor, build);
                } else {
                    return new Version(major, minor, build, revision);
                }
            } else if (bsonType == BsonType.String) {
                return new Version(bsonReader.ReadString());
            } else {
                var message = string.Format("Cannot deserialize Version from BsonType: {0}", bsonType);
                throw new FileFormatException(message);
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
                var version = (Version) value;
                switch (representation) {
                    case BsonType.Document:
                        bsonWriter.WriteStartDocument();
                        bsonWriter.WriteInt32("Major", version.Major);
                        bsonWriter.WriteInt32("Minor", version.Minor);
                        if (version.Build != -1) {
                            bsonWriter.WriteInt32("Build", version.Build);
                            if (version.Revision != -1) {
                                bsonWriter.WriteInt32("Revision", version.Revision);
                            }
                        }
                        bsonWriter.WriteEndDocument();
                        break;
                    case BsonType.String:
                        bsonWriter.WriteString(version.ToString());
                        break;
                    default:
                        throw new BsonInternalException("Unexpected representation");
                }
            }
        }
        #endregion
    }
}
