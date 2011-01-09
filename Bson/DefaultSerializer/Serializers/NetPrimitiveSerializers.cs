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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            BitArray bitArray;
            byte[] bytes;
            BsonBinarySubType subType;
            string message;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Binary:
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary) {
                        message = string.Format("Invalid Binary sub type: {0}", subType);
                        throw new FileFormatException(message);
                    }
                    return new BitArray(bytes);
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var length = bsonReader.ReadInt32("Length");
                    bsonReader.ReadBinaryData("Bytes", out bytes, out subType);
                    if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary) {
                        message = string.Format("Invalid Binary sub type: {0}", subType);
                        throw new FileFormatException(message);
                    }
                    bsonReader.ReadEndDocument();
                    bitArray = new BitArray(bytes);
                    bitArray.Length = length;
                    return bitArray;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    bitArray = new BitArray(s.Length);
                    for (int i = 0; i < s.Length; i++) {
                        var c = s[i];
                        switch (c) {
                            case '0':
                                break;
                            case '1':
                                bitArray[i] = true;
                                break;
                            default:
                                throw new FileFormatException("String value is not a valid BitArray");
                        }
                    }
                    return bitArray;
                default:
                    message = string.Format("Cannot deserialize Byte[] from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }
        #pragma warning restore 618

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bitArray = (BitArray) value;
                var representation = (options == null) ? BsonType.Binary : ((RepresentationSerializationOptions) options).Representation;
                switch (representation) {
                    case BsonType.Binary:
                        if ((bitArray.Length % 8) == 0) {
                            bsonWriter.WriteBinaryData(GetBytes(bitArray), BsonBinarySubType.Binary);
                        } else {
                            bsonWriter.WriteStartDocument();
                            bsonWriter.WriteInt32("Length", bitArray.Length);
                            bsonWriter.WriteBinaryData("Bytes", GetBytes(bitArray), BsonBinarySubType.Binary);
                            bsonWriter.WriteEndDocument();
                        }
                        break;
                    case BsonType.String:
                        var sb = new StringBuilder(bitArray.Length);
                        for (int i = 0; i < bitArray.Length; i++) {
                            sb.Append(bitArray[i] ? '1' : '0');
                        }
                        bsonWriter.WriteString(sb.ToString());
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid representation for type 'BitArray'", representation);
                        throw new BsonSerializationException(message);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            byte[] bytes;
            string message;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Binary:
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary) {
                        message = string.Format("Invalid Binary sub type: {0}", subType);
                        throw new FileFormatException(message);
                    }
                    return bytes;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if ((s.Length % 2) != 0) {
                        s = "0" + s; // prepend a zero to make length even
                    }
                    bytes = new byte[s.Length / 2];
                    for (int i = 0; i < s.Length; i += 2) {
                        var hex = s.Substring(i, 2);
                        var b = byte.Parse(hex, NumberStyles.HexNumber);
                        bytes[i / 2] = b;
                    }
                    return bytes;
                default:
                    message = string.Format("Cannot deserialize Byte[] from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }
        #pragma warning restore 618

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bytes = (byte[]) value;
                var representation = (options == null) ? BsonType.Binary : ((RepresentationSerializationOptions) options).Representation;
                switch (representation) {
                    case BsonType.Binary:
                        bsonWriter.WriteBinaryData(bytes, BsonBinarySubType.Binary);
                        break;
                    case BsonType.String:
                        var sb = new StringBuilder(bytes.Length * 2);
                        for (int i = 0; i < bytes.Length; i++) {
                            sb.Append(string.Format("{0:x2}", bytes[i]));
                        }
                        bsonWriter.WriteString(sb.ToString());
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid representation for type 'Byte[]'", representation);
                        throw new BsonSerializationException(message);
                }
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            byte value;

            var bsonType = bsonReader.CurrentBsonType;
            var lostData = false;
            switch (bsonType) {
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (bytes.Length != 1) {
                        throw new FileFormatException("Binary data for Byte must be exactly one byte long");
                    }
                    value = bytes[0];
                    break;
                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    value = (byte) int32Value;
                    lostData = (int) value != int32Value;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (byte) int64Value;
                    lostData = (int) value != int64Value;
                    break;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if (s.Length == 1) {
                        s = "0" + s;
                    }
                    value = byte.Parse(s, NumberStyles.HexNumber);
                    break;
                default:
                    var message = string.Format("Cannot deserialize Byte from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to Byte", bsonType);
                throw new FileFormatException(message);
            }

            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var byteValue = (byte) value;
            var representation = (options == null) ? BsonType.Int32 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Binary:
                    bsonWriter.WriteBinaryData(new byte[] { byteValue }, BsonBinarySubType.Binary);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(byteValue);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(byteValue);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(string.Format("{0:x2}", byteValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Byte'", representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class CharSerializer : BsonBaseSerializer {
        #region private static fields
        private static CharSerializer singleton = new CharSerializer();
        #endregion

        #region constructors
        private CharSerializer() {
        }
        #endregion

        #region public static properties
        public static CharSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(char), singleton);
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
                case BsonType.Int32:
                    return (char) bsonReader.ReadInt32();
                case BsonType.String:
                    return (char) bsonReader.ReadString()[0];
                default:
                    var message = string.Format("Cannot deserialize Char from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var charValue = (char) value;
            var representation = (options == null) ? BsonType.Int32 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Int32:
                    bsonWriter.WriteInt32((int) charValue);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(new string(new[] { charValue }));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Char'", representation);
                    throw new BsonSerializationException(message);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var name = bsonReader.ReadString("Name");
                    var useUserOverride = bsonReader.ReadBoolean("UseUserOverride");
                    bsonReader.ReadEndDocument();
                    return new CultureInfo(name, useUserOverride);
                case BsonType.String:
                    return new CultureInfo(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize CultureInfo from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
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
        private static DateTimeOffsetSerializer singleton = new DateTimeOffsetSerializer();
        #endregion

        #region constructors
        private DateTimeOffsetSerializer() {
        }
        #endregion

        #region public static properties
        public static DateTimeOffsetSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(DateTimeOffset), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            long ticks;
            TimeSpan offset;
            switch (bsonType) {
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
                    var message = string.Format("Cannot deserialize DateTimeOffset from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC
            var dateTimeOffset = (DateTimeOffset) value;
            var representation = (options == null) ? BsonType.Array : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    bsonWriter.WriteInt64(dateTimeOffset.Ticks);
                    bsonWriter.WriteInt32((int) dateTimeOffset.Offset.TotalMinutes);
                    bsonWriter.WriteEndArray();
                    break;
                case BsonType.Document:
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteDateTime("DateTime", dateTimeOffset.UtcDateTime);
                    bsonWriter.WriteInt64("Ticks", dateTimeOffset.Ticks);
                    bsonWriter.WriteInt32("Offset", (int) dateTimeOffset.Offset.TotalMinutes);
                    bsonWriter.WriteEndDocument();
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(dateTimeOffset));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'DateTimeOffset'", representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class DecimalSerializer : BsonBaseSerializer {
        #region private static fields
        private static DecimalSerializer singleton = new DecimalSerializer();
        #endregion

        #region constructors
        private DecimalSerializer() {
        }
        #endregion

        #region public static properties
        public static DecimalSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Decimal), singleton);
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
                case BsonType.Array:
                    var array = BsonArray.ReadFrom(bsonReader);
                    var bits = new int[4];
                    bits[0] = array[0].AsInt32;
                    bits[1] = array[1].AsInt32;
                    bits[2] = array[2].AsInt32;
                    bits[3] = array[3].AsInt32;
                    return new decimal(bits);
                case BsonType.String:
                    return XmlConvert.ToDecimal(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Decimal from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var decimalValue = (Decimal) value;
            var representation = (options == null) ? BsonType.String : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    var bits = Decimal.GetBits(decimalValue);
                    bsonWriter.WriteInt32(bits[0]);
                    bsonWriter.WriteInt32(bits[1]);
                    bsonWriter.WriteInt32(bits[2]);
                    bsonWriter.WriteInt32(bits[3]);
                    bsonWriter.WriteEndArray();
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(decimalValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Decimal'", representation);
                    throw new BsonSerializationException(message);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            short value;

            var bsonType = bsonReader.CurrentBsonType;
            var lostData = false;
            switch (bsonType) {
                case BsonType.Double:
                    var doubleValue = bsonReader.ReadDouble();
                    value = (short) doubleValue;
                    lostData = (double) value != doubleValue;
                    break;
                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    value = (short) int32Value;
                    lostData = (int) value != int32Value;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (short) int64Value;
                    lostData = (long) value != int64Value;
                    break;
                case BsonType.String:
                    value = XmlConvert.ToInt16(bsonReader.ReadString());
                    break;
                default:
                    var message = string.Format("Cannot deserialize Int16 from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to Int16", bsonType);
                throw new FileFormatException(message);
            }

            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var int16Value = (short) value;
            var representation = (options == null) ? BsonType.Int32 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(int16Value);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(int16Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(int16Value);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(int16Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Int16'", representation);
                    throw new BsonSerializationException(message);
            }
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            sbyte value;

            var bsonType = bsonReader.CurrentBsonType;
            var lostData = false;
            switch (bsonType) {
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (bytes.Length != 1) {
                        throw new FileFormatException("Binary data for SByte must be exactly one byte long");
                    }
                    value = (sbyte) bytes[0];
                    break;
                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    value = (sbyte) int32Value;
                    lostData = (int) value != int32Value;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (sbyte) int64Value;
                    lostData = (int) value != int64Value;
                    break;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if (s.Length == 1) {
                        s = "0" + s;
                    }
                    value = sbyte.Parse(s, NumberStyles.HexNumber);
                    break;
                default:
                    var message = string.Format("Cannot deserialize SByte from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to SByte", bsonType);
                throw new FileFormatException(message);
            }

            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var sbyteValue = (sbyte) value;
            var representation = (options == null) ? BsonType.Int32 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Binary:
                    bsonWriter.WriteBinaryData(new byte[] { (byte) sbyteValue }, BsonBinarySubType.Binary);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(sbyteValue);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(sbyteValue);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(string.Format("{0:x2}", sbyteValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Byte'", representation);
                    throw new BsonSerializationException(message);
            }
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            double doubleValue;

            var bsonType = bsonReader.CurrentBsonType;
            bool lostData = false;
            switch (bsonType) {
                case BsonType.Double:
                    doubleValue = bsonReader.ReadDouble();
                    break;
                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    doubleValue = int32Value;
                    lostData = (int) doubleValue != int32Value;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    doubleValue = int64Value;
                    lostData = (long) doubleValue != int64Value;
                    break;
                case BsonType.String:
                    doubleValue = XmlConvert.ToDouble(bsonReader.ReadString());
                    break;
                default:
                    var message = string.Format("Cannot deserialize Single from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to Single", bsonType);
                throw new FileFormatException(message);
            }

            var floatValue = (doubleValue == double.MinValue) ? float.MinValue : (doubleValue == double.MaxValue) ? float.MaxValue : (float) doubleValue;
            return floatValue;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var floatValue = (float) value;
            var doubleValue = (floatValue == float.MinValue) ? double.MinValue : (floatValue == float.MaxValue) ? double.MaxValue : floatValue;
            var representation = (options == null) ? BsonType.Double : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(doubleValue);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32((int) doubleValue);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64((long) doubleValue);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(doubleValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'Single'", representation);
                    throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    public class TimeSpanSerializer : BsonBaseSerializer {
        #region private static fields
        private static TimeSpanSerializer singleton = new TimeSpanSerializer();
        #endregion

        #region constructors
        private TimeSpanSerializer() {
        }
        #endregion

        #region public static properties
        public static TimeSpanSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(TimeSpan), singleton);
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
                case BsonType.Int32:
                    return new TimeSpan((long) bsonReader.ReadInt32());
                case BsonType.Int64:
                    return new TimeSpan(bsonReader.ReadInt64());
                case BsonType.String:
                     return TimeSpan.Parse(bsonReader.ReadString()); // not XmlConvert.ToTimeSpan (we're using .NET's format for TimeSpan)
                default:
                    var message = string.Format("Cannot deserialize TimeSpan from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var timeSpan = (TimeSpan) value;
            var representation = (options == null) ? BsonType.String : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Int64:
                    bsonWriter.WriteInt64(timeSpan.Ticks);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(timeSpan.ToString()); // for TimeSpan use .NET's format instead of XmlConvert.ToString
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'TimeSpan'", representation);
                    throw new BsonSerializationException(message);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            ushort value;

            var bsonType = bsonReader.CurrentBsonType;
            var lostData = false;
            switch (bsonType) {
                case BsonType.Double:
                    var doubleValue = bsonReader.ReadDouble();
                    value = (ushort) doubleValue;
                    lostData = (double) value != doubleValue;
                    break;
                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    value = (ushort) int32Value;
                    lostData = (int) value != int32Value;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (ushort) int64Value;
                    lostData = (long) value != int64Value;
                    break;
                case BsonType.String:
                    value = XmlConvert.ToUInt16(bsonReader.ReadString());
                    break;
                default:
                    var message = string.Format("Cannot deserialize uInt16 from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to uInt16", bsonType);
                throw new FileFormatException(message);
            }

            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var uint16Value = (ushort) value;
            var representation = (options == null) ? BsonType.Int32 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(uint16Value);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(uint16Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(uint16Value);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(uint16Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'UInt16'", representation);
                    throw new BsonSerializationException(message);
            }
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            uint value;

            var bsonType = bsonReader.CurrentBsonType;
            var lostData = false;
            switch (bsonType) {
                case BsonType.Double:
                    var doubleValue = bsonReader.ReadDouble();
                    value = (uint) doubleValue;
                    lostData = (double) value != doubleValue;
                    break;
                case BsonType.Int32:
                    value = (uint) bsonReader.ReadInt32();
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (uint) int64Value;
                    lostData = (long) value != int64Value;
                    break;
                case BsonType.String:
                    value = XmlConvert.ToUInt32(bsonReader.ReadString());
                    break;
                default:
                    var message = string.Format("Cannot deserialize uInt32 from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to uInt32", bsonType);
                throw new FileFormatException(message);
            }

            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var uint32Value = (uint) value;
            var representation = (options == null) ? BsonType.Int32 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(uint32Value);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32((int) uint32Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(uint32Value);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(uint32Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'UInt32'", representation);
                    throw new BsonSerializationException(message);
            }
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            ulong value;

            var bsonType = bsonReader.CurrentBsonType;
            var lostData = false;
            switch (bsonType) {
                case BsonType.Double:
                    var doubleValue = bsonReader.ReadDouble();
                    value = (ulong) doubleValue;
                    lostData = (double) value != doubleValue;
                    break;
                case BsonType.Int32:
                    value = (ulong) bsonReader.ReadInt32();
                    break;
                case BsonType.Int64:
                    value = (ulong) bsonReader.ReadInt64();
                    break;
                case BsonType.String:
                    value = XmlConvert.ToUInt64(bsonReader.ReadString());
                    break;
                default:
                    var message = string.Format("Cannot deserialize UInt64 from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData) {
                var message = string.Format("Data loss occurred when trying to convert from {0} to UInt64", bsonType);
                throw new FileFormatException(message);
            }

            return value;
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var uint64Value = (ulong) value;
            var representation = (options == null) ? BsonType.Int64 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(uint64Value);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32((int) uint64Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64((long) uint64Value);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(uint64Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'UInt64'", representation);
                    throw new BsonSerializationException(message);
            }
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.String:
                    return new Uri(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Uri from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
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
                bsonWriter.WriteString(((Uri) value).AbsoluteUri);
            }
        }
        #endregion
    }

    public class VersionSerializer : BsonBaseSerializer {
        #region private static fields
        private static VersionSerializer singleton = new VersionSerializer();
        #endregion

        #region constructors
        private VersionSerializer() {
        }
        #endregion

        #region public static properties
        public static VersionSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Version), singleton);
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            string message;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Document:
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
                                message = string.Format("Unrecognized element in Version: {0}", name);
                                throw new FileFormatException(message);
                        }
                    }
                    bsonReader.ReadEndDocument();
                    if (major == -1) {
                        message = string.Format("Version missing Major element");
                        throw new FileFormatException(message);
                    } else if (minor == -1) {
                        message = string.Format("Version missing Minor element");
                        throw new FileFormatException(message);
                    } else if (build == -1) {
                        return new Version(major, minor);
                    } else if (revision == -1) {
                        return new Version(major, minor, build);
                    } else {
                        return new Version(major, minor, build, revision);
                    }
                case BsonType.String:
                    return new Version(bsonReader.ReadString());
                default:
                    message = string.Format("Cannot deserialize Version from BsonType: {0}", bsonType);
                    throw new FileFormatException(message);
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
                var version = (Version) value;
                var representation = (options == null) ? BsonType.String : ((RepresentationSerializationOptions) options).Representation;
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
                        var message = string.Format("'{0}' is not a valid representation for type 'Version'", representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
        #endregion
    }
}
