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

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.DefaultSerializer {
    public class ByteSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (byte) bsonReader.ReadInt32(out name);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(byte).FullName);
                var value = (byte) bsonReader.ReadInt32("_v");
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.Byte");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (byte) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteInt32(name, value);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(byte).FullName);
                bsonWriter.WriteInt32("_v", value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class CharSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.String) {
                return (char) bsonReader.ReadString(out name)[0];
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(char).FullName);
                var value = bsonReader.ReadString("_v")[0];
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.Char");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = new string(new char[] { (char) obj });
            if (useCompactRepresentation) {
                bsonWriter.WriteString(name, value);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(char).FullName);
                bsonWriter.WriteString("_v", value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class CultureInfoSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            bsonReader.ReadDocumentName(out name);
            bsonReader.ReadStartDocument();
            bsonReader.VerifyString("_t", typeof(CultureInfo).FullName);
            var value = new CultureInfo(bsonReader.ReadString("_v"));
            bsonReader.ReadEndDocument();
            return value;
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (CultureInfo) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                if (useCompactRepresentation) {
                    bsonWriter.WriteString(name, value.ToString());
                } else {
                    bsonWriter.WriteDocumentName(name);
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", typeof(CultureInfo).FullName);
                    bsonWriter.WriteString("_v", value.ToString());
                    bsonWriter.WriteEndDocument();
                }
            }
        }
        #endregion
    }

    public class DateTimeOffsetSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
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
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(DateTimeOffset).FullName);
                var dateTime = DateTime.Parse(bsonReader.ReadString("dt")); // Kind = DateTimeKind.Unspecified
                var offset = TimeSpan.Parse(bsonReader.ReadString("o"));
                bsonReader.ReadEndDocument();
                return new DateTimeOffset(dateTime, offset);
            } else {
                throw new FileFormatException("Element is not valid System.DateTimeOffset");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC
            var value = (DateTimeOffset) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteArrayName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteInt64("0", value.DateTime.Ticks);
                bsonWriter.WriteInt32("1", (int) value.Offset.TotalMinutes);
                bsonWriter.WriteEndDocument();
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(DateTimeOffset).FullName);
                bsonWriter.WriteString("dt", value.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF")); // omit trailing zeros
                bsonWriter.WriteString("o", Regex.Replace(value.Offset.ToString(), ":00$", "")); // omit trailing zeros
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class DecimalSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            decimal value;
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

            } else {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(Decimal).FullName);
                value = XmlConvert.ToDecimal(bsonReader.ReadString("_v"));
                bsonReader.ReadEndDocument();
                return value;
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (Decimal) obj;
            if (useCompactRepresentation) {
                var bits = Decimal.GetBits(value);
                bsonWriter.WriteArrayName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteInt32("0", bits[0]);
                bsonWriter.WriteInt32("1", bits[1]);
                bsonWriter.WriteInt32("2", bits[2]);
                bsonWriter.WriteInt32("3", bits[3]);
                bsonWriter.WriteEndDocument();
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(Decimal).FullName);
                bsonWriter.WriteString("_v", XmlConvert.ToString(value));
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class Int16Serializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (short) bsonReader.ReadInt32(out name);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(short).FullName);
                var value = (short) bsonReader.ReadInt32("_v");
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.Int16");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (short) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteInt32(name, value);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(short).FullName);
                bsonWriter.WriteInt32("_v", (int) value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class SByteSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (sbyte) bsonReader.ReadInt32(out name);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(sbyte).FullName);
                var value = (sbyte) bsonReader.ReadInt32("_v");
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.SByte");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (sbyte) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteInt32(name, (int) value);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(sbyte).FullName);
                bsonWriter.WriteInt32("_v", (int) value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class SingleSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            double doubleValue;
            if (bsonType == BsonType.Double) {
                doubleValue = bsonReader.ReadDouble(out name);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(float).FullName);
                doubleValue = bsonReader.ReadDouble("_v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.Single");
            }
            return doubleValue == double.MinValue ? float.MinValue : doubleValue == double.MaxValue ? float.MaxValue : (float) doubleValue;
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (float) obj;
            var doubleValue = value == float.MinValue ? double.MinValue : value == float.MaxValue ? double.MaxValue : value;
            if (useCompactRepresentation) {
                bsonWriter.WriteDouble(name, doubleValue);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(float).FullName);
                bsonWriter.WriteDouble("_v", doubleValue);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class TimeSpanSerializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int64) {
                return new TimeSpan(bsonReader.ReadInt64(out name));
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(TimeSpan).FullName);
                var value = TimeSpan.Parse(bsonReader.ReadString("_v"));
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.TimeSpan");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (TimeSpan) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteInt64(name, value.Ticks);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(TimeSpan).FullName);
                bsonWriter.WriteString("_v", value.ToString());
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class UInt16Serializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (ushort) bsonReader.ReadInt32(out name);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(ushort).FullName);
                var value = (ushort) bsonReader.ReadInt32("_v");
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.UInt16");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (ushort) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteInt32(name, value);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(ushort).FullName);
                bsonWriter.WriteInt32("_v", value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class UInt32Serializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int32) {
                return (uint) bsonReader.ReadInt32(out name);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(uint).FullName);
                var value = (uint) bsonReader.ReadInt32("_v");
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.UInt32");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (uint) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteInt32(name, (int) value);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(uint).FullName);
                bsonWriter.WriteInt32("_v", (int) value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class UInt64Serializer : IBsonSerializer {
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
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Int64) {
                return (ulong) bsonReader.ReadInt64(out name);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(ulong).FullName);
                var value = (ulong) bsonReader.ReadInt64("_v");
                bsonReader.ReadEndDocument();
                return value;
            } else {
                throw new FileFormatException("Element is not valid System.UInt64");
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            throw new InvalidOperationException();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (ulong) obj;
            if (useCompactRepresentation) {
                bsonWriter.WriteInt64(name, (long) value);
            } else {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(ulong).FullName);
                bsonWriter.WriteInt64("_v", (long) value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }
}
