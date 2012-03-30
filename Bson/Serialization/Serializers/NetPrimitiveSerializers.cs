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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BitArrays.
    /// </summary>
    public class BitArraySerializer : BsonBaseSerializer
    {
        // private static fields
        private static BitArraySerializer __instance = new BitArraySerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BitArraySerializer class.
        /// </summary>
        public BitArraySerializer()
            : base(new RepresentationSerializationOptions(BsonType.Binary))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BitArraySerializer class.
        /// </summary>
        public static BitArraySerializer Instance
        {
            get { return __instance; }
        }

        // public methods
#pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
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
            VerifyTypes(nominalType, actualType, typeof(BitArray));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            BitArray bitArray;
            byte[] bytes;
            BsonBinarySubType subType;
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Binary:
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary)
                    {
                        message = string.Format("Invalid Binary sub type {0}.", subType);
                        throw new FileFormatException(message);
                    }
                    return new BitArray(bytes);
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var length = bsonReader.ReadInt32("Length");
                    bsonReader.ReadBinaryData("Bytes", out bytes, out subType);
                    if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary)
                    {
                        message = string.Format("Invalid Binary sub type {0}.", subType);
                        throw new FileFormatException(message);
                    }
                    bsonReader.ReadEndDocument();
                    bitArray = new BitArray(bytes);
                    bitArray.Length = length;
                    return bitArray;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    bitArray = new BitArray(s.Length);
                    for (int i = 0; i < s.Length; i++)
                    {
                        var c = s[i];
                        switch (c)
                        {
                            case '0':
                                break;
                            case '1':
                                bitArray[i] = true;
                                break;
                            default:
                                throw new FileFormatException("String value is not a valid BitArray.");
                        }
                    }
                    return bitArray;
                default:
                    message = string.Format("Cannot deserialize Byte[] from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }
#pragma warning restore 618

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
                var bitArray = (BitArray)value;
                var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

                switch (representationSerializationOptions.Representation)
                {
                    case BsonType.Binary:
                        if ((bitArray.Length % 8) == 0)
                        {
                            bsonWriter.WriteBinaryData(GetBytes(bitArray), BsonBinarySubType.Binary);
                        }
                        else
                        {
                            bsonWriter.WriteStartDocument();
                            bsonWriter.WriteInt32("Length", bitArray.Length);
                            bsonWriter.WriteBinaryData("Bytes", GetBytes(bitArray), BsonBinarySubType.Binary);
                            bsonWriter.WriteEndDocument();
                        }
                        break;
                    case BsonType.String:
                        var sb = new StringBuilder(bitArray.Length);
                        for (int i = 0; i < bitArray.Length; i++)
                        {
                            sb.Append(bitArray[i] ? '1' : '0');
                        }
                        bsonWriter.WriteString(sb.ToString());
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid BitArray representation.", representationSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }

        // private methods
        private byte[] GetBytes(BitArray bitArray)
        {
            // TODO: is there a more efficient way to do this?
            var bytes = new byte[(bitArray.Length + 7) / 8];
            var i = 0;
            foreach (bool value in bitArray)
            {
                if (value)
                {
                    var index = i / 8;
                    var bit = i % 8;
                    bytes[index] |= (byte)(1 << bit);
                }
                i++;
            }
            return bytes;
        }
    }

    /// <summary>
    /// Represents a serializer for ByteArrays.
    /// </summary>
    public class ByteArraySerializer : BsonBaseSerializer
    {
        // private static fields
        private static ByteArraySerializer __instance = new ByteArraySerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the ByteArraySerializer class.
        /// </summary>
        public ByteArraySerializer()
            : base(new RepresentationSerializationOptions(BsonType.Binary))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the ByteArraySerializer class.
        /// </summary>
        public static ByteArraySerializer Instance
        {
            get { return __instance; }
        }

        // public methods
#pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
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
            VerifyTypes(nominalType, actualType, typeof(byte[]));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            byte[] bytes;
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Binary:
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary)
                    {
                        message = string.Format("Invalid Binary sub type {0}.", subType);
                        throw new FileFormatException(message);
                    }
                    return bytes;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if ((s.Length % 2) != 0)
                    {
                        s = "0" + s; // prepend a zero to make length even
                    }
                    bytes = new byte[s.Length / 2];
                    for (int i = 0; i < s.Length; i += 2)
                    {
                        var hex = s.Substring(i, 2);
                        var b = byte.Parse(hex, NumberStyles.HexNumber);
                        bytes[i / 2] = b;
                    }
                    return bytes;
                default:
                    message = string.Format("Cannot deserialize Byte[] from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }
#pragma warning restore 618

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
                var bytes = (byte[])value;
                var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

                switch (representationSerializationOptions.Representation)
                {
                    case BsonType.Binary:
                        bsonWriter.WriteBinaryData(bytes, BsonBinarySubType.Binary);
                        break;
                    case BsonType.String:
                        var sb = new StringBuilder(bytes.Length * 2);
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            sb.Append(string.Format("{0:x2}", bytes[i]));
                        }
                        bsonWriter.WriteString(sb.ToString());
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid Byte[] representation.", representationSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Bytes.
    /// </summary>
    public class ByteSerializer : BsonBaseSerializer
    {
        // private static fields
        private static ByteSerializer __instance = new ByteSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the ByteSerializer class.
        /// </summary>
        public ByteSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Int32))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the ByteSerializer class.
        /// </summary>
        public static ByteSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(byte));

            var bsonType = bsonReader.GetCurrentBsonType();
            byte value;
            var lostData = false;
            switch (bsonType)
            {
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (bytes.Length != 1)
                    {
                        throw new FileFormatException("Binary data for Byte must be exactly one byte long.");
                    }
                    value = bytes[0];
                    break;
                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    value = (byte)int32Value;
                    lostData = (int)value != int32Value;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (byte)int64Value;
                    lostData = (int)value != int64Value;
                    break;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if (s.Length == 1)
                    {
                        s = "0" + s;
                    }
                    value = byte.Parse(s, NumberStyles.HexNumber);
                    break;
                default:
                    var message = string.Format("Cannot deserialize Byte from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData)
            {
                var message = string.Format("Data loss occurred when trying to convert from {0} to Byte.", bsonType);
                throw new FileFormatException(message);
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
            var byteValue = (byte)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
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
                    var message = string.Format("'{0}' is not a valid Byte representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Chars.
    /// </summary>
    public class CharSerializer : BsonBaseSerializer
    {
        // private static fields
        private static CharSerializer __instance = new CharSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the CharSerializer class.
        /// </summary>
        public CharSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Int32))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the CharSerializer class.
        /// </summary>
        public static CharSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(char));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Int32:
                    return (char)bsonReader.ReadInt32();
                case BsonType.String:
                    return (char)bsonReader.ReadString()[0];
                default:
                    var message = string.Format("Cannot deserialize Char from BsonType {0}.", bsonType);
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
            var charValue = (char)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Int32:
                    bsonWriter.WriteInt32((int)charValue);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(new string(new[] { charValue }));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Char representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for CultureInfos.
    /// </summary>
    public class CultureInfoSerializer : BsonBaseSerializer
    {
        // private static fields
        private static CultureInfoSerializer __instance = new CultureInfoSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the CultureInfoSerializer class.
        /// </summary>
        public CultureInfoSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the CultureInfoSerializer class.
        /// </summary>
        public static CultureInfoSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(CultureInfo));

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
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
                    var message = string.Format("Cannot deserialize CultureInfo from BsonType {0}.", bsonType);
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
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var cultureInfo = (CultureInfo)value;
                if (cultureInfo.UseUserOverride)
                {
                    // the default for UseUserOverride is true so we don't need to serialize it
                    bsonWriter.WriteString(cultureInfo.Name);
                }
                else
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("Name", cultureInfo.Name);
                    bsonWriter.WriteBoolean("UseUserOverride", cultureInfo.UseUserOverride);
                    bsonWriter.WriteEndDocument();
                }
            }
        }
    }

    /// <summary>
    /// Represents a serializer for DateTimeOffsets.
    /// </summary>
    public class DateTimeOffsetSerializer : BsonBaseSerializer
    {
        // private static fields
        private static DateTimeOffsetSerializer __instance = new DateTimeOffsetSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the DateTimeOffsetSerializer class.
        /// </summary>
        public DateTimeOffsetSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Array))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the DateTimeOffsetSerializer class.
        /// </summary>
        public static DateTimeOffsetSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(DateTimeOffset));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            long ticks;
            TimeSpan offset;
            switch (bsonType)
            {
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
                    var message = string.Format("Cannot deserialize DateTimeOffset from BsonType {0}.", bsonType);
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
            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC
            var dateTimeOffset = (DateTimeOffset)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    bsonWriter.WriteInt64(dateTimeOffset.Ticks);
                    bsonWriter.WriteInt32((int)dateTimeOffset.Offset.TotalMinutes);
                    bsonWriter.WriteEndArray();
                    break;
                case BsonType.Document:
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteDateTime("DateTime", BsonUtils.ToMillisecondsSinceEpoch(dateTimeOffset.UtcDateTime));
                    bsonWriter.WriteInt64("Ticks", dateTimeOffset.Ticks);
                    bsonWriter.WriteInt32("Offset", (int)dateTimeOffset.Offset.TotalMinutes);
                    bsonWriter.WriteEndDocument();
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(dateTimeOffset));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid DateTimeOffset representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Decimals.
    /// </summary>
    public class DecimalSerializer : BsonBaseSerializer
    {
        // private static fields
        private static DecimalSerializer __instance = new DecimalSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the DecimalSerializer class.
        /// </summary>
        public DecimalSerializer()
            : base(new RepresentationSerializationOptions(BsonType.String))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the DecimalSerializer class.
        /// </summary>
        public static DecimalSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(decimal));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Array:
                    var array = BsonArray.ReadFrom(bsonReader);
                    var bits = new int[4];
                    bits[0] = array[0].AsInt32;
                    bits[1] = array[1].AsInt32;
                    bits[2] = array[2].AsInt32;
                    bits[3] = array[3].AsInt32;
                    return new decimal(bits);
                case BsonType.Double:
                    return representationSerializationOptions.ToDecimal(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationSerializationOptions.ToDecimal(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationSerializationOptions.ToDecimal(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToDecimal(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Decimal from BsonType {0}.", bsonType);
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
            var decimalValue = (Decimal)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    var bits = Decimal.GetBits(decimalValue);
                    bsonWriter.WriteInt32(bits[0]);
                    bsonWriter.WriteInt32(bits[1]);
                    bsonWriter.WriteInt32(bits[2]);
                    bsonWriter.WriteInt32(bits[3]);
                    bsonWriter.WriteEndArray();
                    break;
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(decimalValue));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(decimalValue));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(decimalValue));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(decimalValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Decimal representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }


    /// <summary>
    /// Represents a serializer for System.Drawing.Size.
    /// </summary>
    public class DrawingSizeSerializer : BsonBaseSerializer
    {
        // private static fields
        private static DrawingSizeSerializer __instance = new DrawingSizeSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the DrawingSizeSerializer class.
        /// </summary>
        public DrawingSizeSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the DrawingSizeSerializer class.
        /// </summary>
        public static DrawingSizeSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object of type System.Drawing.Size from a BsonReader.
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
            VerifyTypes(nominalType, actualType, typeof(System.Drawing.Size));

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var width = bsonReader.ReadInt32("Width");
                    var height = bsonReader.ReadInt32("Height");
                    bsonReader.ReadEndDocument();
                    return new System.Drawing.Size(width, height);
                default:
                    var message = string.Format("Cannot deserialize Size from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object of type System.Drawing.Size  to a BsonWriter.
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
            var size = (System.Drawing.Size)value;
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteInt32("Width", size.Width);
            bsonWriter.WriteInt32("Height", size.Height);
            bsonWriter.WriteEndDocument();
        }
    }

    /// <summary>
    /// Represents a serializer for Int16s.
    /// </summary>
    public class Int16Serializer : BsonBaseSerializer
    {
        // private static fields
        private static Int16Serializer __instance = new Int16Serializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the Int16Serializer class.
        /// </summary>
        public Int16Serializer()
            : base(new RepresentationSerializationOptions(BsonType.Int32))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the Int16Serializer class.
        /// </summary>
        public static Int16Serializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(short));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return representationSerializationOptions.ToInt16(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationSerializationOptions.ToInt16(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationSerializationOptions.ToInt16(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToInt16(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Int16 from BsonType {0}.", bsonType);
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
            var int16Value = (short)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(int16Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(int16Value));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(int16Value));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(int16Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Int16 representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for IPAddresses.
    /// </summary>
    public class IPAddressSerializer : BsonBaseSerializer
    {
        // private static fields
        private static IPAddressSerializer __instance = new IPAddressSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the IPAddressSerializer class.
        /// </summary>
        public IPAddressSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the IPAddressSerializer class.
        /// </summary>
        public static IPAddressSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(IPAddress));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.String:
                    var stringValue = bsonReader.ReadString();
                    IPAddress address;
                    if (IPAddress.TryParse(stringValue, out address))
                    {
                        return address;
                    }
                    message = string.Format("Invalid IPAddress value '{0}'.", stringValue);
                    throw new FileFormatException(message);
                default:
                    message = string.Format("Cannot deserialize IPAddress from BsonType {0}.", bsonType);
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
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var address = (IPAddress)value;
                string stringValue;
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    stringValue = address.ToString();
                }
                else
                {
                    stringValue = string.Format("[{0}]", address);
                }
                bsonWriter.WriteString(stringValue);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for IPEndPoints.
    /// </summary>
    public class IPEndPointSerializer : BsonBaseSerializer
    {
        // private static fields
        private static IPEndPointSerializer __instance = new IPEndPointSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the IPEndPointSerializer class.
        /// </summary>
        public IPEndPointSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the IPEndPointSerializer class.
        /// </summary>
        public static IPEndPointSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(IPEndPoint));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.String:
                    var stringValue = bsonReader.ReadString();
                    var match = Regex.Match(stringValue, @"^(?<address>(.+|\[.*\]))\:(?<port>\d+)$");
                    if (match.Success)
                    {
                        IPAddress address;
                        if (IPAddress.TryParse(match.Groups["address"].Value, out address))
                        {
                            int port;
                            if (int.TryParse(match.Groups["port"].Value, out port))
                            {
                                return new IPEndPoint(address, port);
                            }
                        }
                    }
                    message = string.Format("Invalid IPEndPoint value '{0}'.", stringValue);
                    throw new FileFormatException(message);
                default:
                    message = string.Format("Cannot deserialize IPEndPoint from BsonType {0}.", bsonType);
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
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var endPoint = (IPEndPoint)value;
                string stringValue;
                if (endPoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    stringValue = string.Format("{0}:{1}", endPoint.Address, endPoint.Port); // IPv4
                }
                else
                {
                    stringValue = string.Format("[{0}]:{1}", endPoint.Address, endPoint.Port); // IPv6
                }
                bsonWriter.WriteString(stringValue);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for SBytes.
    /// </summary>
    public class SByteSerializer : BsonBaseSerializer
    {
        // private static fields
        private static SByteSerializer __instance = new SByteSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the SByteSerializer class.
        /// </summary>
        public SByteSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Int32))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the SByteSerializer class.
        /// </summary>
        public static SByteSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(sbyte));

            var bsonType = bsonReader.GetCurrentBsonType();
            var lostData = false;
            sbyte value;
            switch (bsonType)
            {
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    if (bytes.Length != 1)
                    {
                        throw new FileFormatException("Binary data for SByte must be exactly one byte long.");
                    }
                    value = (sbyte)bytes[0];
                    break;
                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    value = (sbyte)int32Value;
                    lostData = (int)value != int32Value;
                    break;
                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (sbyte)int64Value;
                    lostData = (int)value != int64Value;
                    break;
                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if (s.Length == 1)
                    {
                        s = "0" + s;
                    }
                    value = (sbyte)byte.Parse(s, NumberStyles.HexNumber);
                    break;
                default:
                    var message = string.Format("Cannot deserialize SByte from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
            if (lostData)
            {
                var message = string.Format("Data loss occurred when trying to convert from {0} to SByte.", bsonType);
                throw new FileFormatException(message);
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
            var sbyteValue = (sbyte)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Binary:
                    bsonWriter.WriteBinaryData(new byte[] { (byte)sbyteValue }, BsonBinarySubType.Binary);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(sbyteValue);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(sbyteValue);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(string.Format("{0:x2}", (byte)sbyteValue));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid SByte representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Singles.
    /// </summary>
    public class SingleSerializer : BsonBaseSerializer
    {
        // private static fields
        private static SingleSerializer __instance = new SingleSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the SingleSerializer class.
        /// </summary>
        public SingleSerializer()
            : base(new RepresentationSerializationOptions(BsonType.Double))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the SingleSerializer class.
        /// </summary>
        public static SingleSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(float));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return representationSerializationOptions.ToSingle(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationSerializationOptions.ToSingle(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationSerializationOptions.ToSingle(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToSingle(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize Single from BsonType {0}.", bsonType);
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
            var floatValue = (float)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(floatValue));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(floatValue));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(floatValue));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(floatValue.ToString("R", NumberFormatInfo.InvariantInfo));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid Single representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

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

    /// <summary>
    /// Represents a serializer for UInt16s.
    /// </summary>
    public class UInt16Serializer : BsonBaseSerializer
    {
        // private static fields
        private static UInt16Serializer __instance = new UInt16Serializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the UInt16Serializer class.
        /// </summary>
        public UInt16Serializer()
            : base(new RepresentationSerializationOptions(BsonType.Int32))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the UInt16Serializer class.
        /// </summary>
        public static UInt16Serializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(ushort));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return representationSerializationOptions.ToUInt16(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationSerializationOptions.ToUInt16(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationSerializationOptions.ToUInt16(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToUInt16(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize uInt16 from BsonType {0}.", bsonType);
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
            var uint16Value = (ushort)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(uint16Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(uint16Value));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(uint16Value));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(uint16Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid representation for type 'UInt16'", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for UInt32s.
    /// </summary>
    public class UInt32Serializer : BsonBaseSerializer
    {
        // private static fields
        private static UInt32Serializer __instance = new UInt32Serializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the UInt32Serializer class.
        /// </summary>
        public UInt32Serializer()
            : base(new RepresentationSerializationOptions(BsonType.Int32))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the UInt32Serializer class.
        /// </summary>
        public static UInt32Serializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(uint));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return representationSerializationOptions.ToUInt32(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationSerializationOptions.ToUInt32(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationSerializationOptions.ToUInt32(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToUInt32(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize UInt32 from BsonType {0}.", bsonType);
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
            var uint32Value = (uint)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(uint32Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(uint32Value));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(uint32Value));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(uint32Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid UInt32 representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for UInt64s.
    /// </summary>
    public class UInt64Serializer : BsonBaseSerializer
    {
        // private static fields
        private static UInt64Serializer __instance = new UInt64Serializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the UInt64Serializer class.
        /// </summary>
        public UInt64Serializer()
            : base(new RepresentationSerializationOptions(BsonType.Int64))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the UInt64Serializer class.
        /// </summary>
        public static UInt64Serializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(ulong));
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Double:
                    return representationSerializationOptions.ToUInt64(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return representationSerializationOptions.ToUInt64(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return representationSerializationOptions.ToUInt64(bsonReader.ReadInt64());
                case BsonType.String:
                    return XmlConvert.ToUInt64(bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize UInt64 from BsonType {0}.", bsonType);
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
            var uint64Value = (ulong)value;
            var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

            switch (representationSerializationOptions.Representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(representationSerializationOptions.ToDouble(uint64Value));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(representationSerializationOptions.ToInt32(uint64Value));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(representationSerializationOptions.ToInt64(uint64Value));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(uint64Value));
                    break;
                default:
                    var message = string.Format("'{0}' is not a valid UInt64 representation.", representationSerializationOptions.Representation);
                    throw new BsonSerializationException(message);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Uris.
    /// </summary>
    public class UriSerializer : BsonBaseSerializer
    {
        // private static fields
        private static UriSerializer __instance = new UriSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the UriSerializer class.
        /// </summary>
        public UriSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the UriSerializer class.
        /// </summary>
        public static UriSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(Uri));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.String:
                    return new Uri(bsonReader.ReadString(), UriKind.RelativeOrAbsolute);
                default:
                    var message = string.Format("Cannot deserialize Uri from BsonType {0}.", bsonType);
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
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                bsonWriter.WriteString(((Uri)value).OriginalString);
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Versions.
    /// </summary>
    public class VersionSerializer : BsonBaseSerializer
    {
        // private static fields
        private static VersionSerializer __instance = new VersionSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the VersionSerializer class.
        /// </summary>
        public VersionSerializer()
            : base(new RepresentationSerializationOptions(BsonType.String))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the VersionSerializer class.
        /// </summary>
        public static VersionSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(Version));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    int major = -1, minor = -1, build = -1, revision = -1;
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = bsonReader.ReadName();
                        switch (name)
                        {
                            case "Major": major = bsonReader.ReadInt32(); break;
                            case "Minor": minor = bsonReader.ReadInt32(); break;
                            case "Build": build = bsonReader.ReadInt32(); break;
                            case "Revision": revision = bsonReader.ReadInt32(); break;
                            default:
                                message = string.Format("Unrecognized element '{0}' while deserializing a Version value.", name);
                                throw new FileFormatException(message);
                        }
                    }
                    bsonReader.ReadEndDocument();
                    if (major == -1)
                    {
                        message = string.Format("Version missing Major element.");
                        throw new FileFormatException(message);
                    }
                    else if (minor == -1)
                    {
                        message = string.Format("Version missing Minor element.");
                        throw new FileFormatException(message);
                    }
                    else if (build == -1)
                    {
                        return new Version(major, minor);
                    }
                    else if (revision == -1)
                    {
                        return new Version(major, minor, build);
                    }
                    else
                    {
                        return new Version(major, minor, build, revision);
                    }
                case BsonType.String:
                    return new Version(bsonReader.ReadString());
                default:
                    message = string.Format("Cannot deserialize Version from BsonType {0}.", bsonType);
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
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var version = (Version)value;
                var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

                switch (representationSerializationOptions.Representation)
                {
                    case BsonType.Document:
                        bsonWriter.WriteStartDocument();
                        bsonWriter.WriteInt32("Major", version.Major);
                        bsonWriter.WriteInt32("Minor", version.Minor);
                        if (version.Build != -1)
                        {
                            bsonWriter.WriteInt32("Build", version.Build);
                            if (version.Revision != -1)
                            {
                                bsonWriter.WriteInt32("Revision", version.Revision);
                            }
                        }
                        bsonWriter.WriteEndDocument();
                        break;
                    case BsonType.String:
                        bsonWriter.WriteString(version.ToString());
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid Version representation.", representationSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
    }
}
