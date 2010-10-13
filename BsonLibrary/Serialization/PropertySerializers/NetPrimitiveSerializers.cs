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

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class BytePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BytePropertySerializer singleton = new BytePropertySerializer();
        #endregion

        #region constructors
        private BytePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BytePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(byte), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            byte value;
            if (bsonType == BsonType.Int32) {
                value = (byte) bsonReader.ReadInt32(propertyMap.ElementName);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(byte).FullName);
                value = (byte) bsonReader.ReadInt32("v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.Byte");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (byte) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteInt32(propertyMap.ElementName, value);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(byte).FullName);
                bsonWriter.WriteInt32("v", value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class CharPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static CharPropertySerializer singleton = new CharPropertySerializer();
        #endregion

        #region constructors
        private CharPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static CharPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(char), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            char value;
            if (bsonType == BsonType.String) {
                value = (char) bsonReader.ReadString(propertyMap.ElementName)[0];
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(char).FullName);
                value = bsonReader.ReadString("v")[0];
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.Char");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (char) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteString(propertyMap.ElementName, new string(new char[] { value }));
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(char).FullName);
                bsonWriter.WriteString("v", new string(new char[] { value }));
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class CultureInfoPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static CultureInfoPropertySerializer singleton = new CultureInfoPropertySerializer();
        #endregion

        #region constructors
        private CultureInfoPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static CultureInfoPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(CultureInfo), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            bsonReader.ReadDocumentName(propertyMap.ElementName);
            bsonReader.ReadStartDocument();
            bsonReader.VerifyString("_t", typeof(CultureInfo).FullName);
            var value = new CultureInfo(bsonReader.ReadString("v"));
            bsonReader.ReadEndDocument();
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (CultureInfo) propertyMap.Getter(obj);
            bsonWriter.WriteDocumentName(propertyMap.ElementName);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", typeof(CultureInfo).FullName);
            bsonWriter.WriteString("v", value.ToString());
            bsonWriter.WriteEndDocument();
        }
        #endregion
    }

    public class DateTimeOffsetPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static DateTimeOffsetPropertySerializer singleton = new DateTimeOffsetPropertySerializer();
        #endregion

        #region constructors
        private DateTimeOffsetPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static DateTimeOffsetPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(DateTimeOffset), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            DateTimeOffset value;
            if (bsonType == BsonType.Array) {
                bsonReader.ReadArrayName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                var dateTime = new DateTime(bsonReader.ReadInt64("0"));
                var offset = new TimeSpan(bsonReader.ReadInt64("1"));
                bsonReader.ReadEndDocument();
                value = new DateTimeOffset(dateTime, offset);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(DateTimeOffset).FullName);
                var dateTime = DateTime.Parse(bsonReader.ReadString("dt")); // Kind = DateTimeKind.Unspecified
                var offset = TimeSpan.Parse(bsonReader.ReadString("o"));
                bsonReader.ReadEndDocument();
                value = new DateTimeOffset(dateTime, offset);
            } else {
                throw new FileFormatException("Element is not valid System.DateTimeOffset");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC
            var value = (DateTimeOffset) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteArrayName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteInt64("0", value.DateTime.Ticks);
                bsonWriter.WriteInt64("1", value.Offset.Ticks);
                bsonWriter.WriteEndDocument();
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(DateTimeOffset).FullName);
                bsonWriter.WriteString("dt", value.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF")); // omit trailing zeros
                bsonWriter.WriteString("o", Regex.Replace(value.Offset.ToString(), ":00$", "")); // omit trailing zeros
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class DecimalPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static DecimalPropertySerializer singleton = new DecimalPropertySerializer();
        #endregion

        #region constructors
        private DecimalPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static DecimalPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(Decimal), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            bsonReader.ReadDocumentName(propertyMap.ElementName);
            bsonReader.ReadStartDocument();
            bsonReader.VerifyString("_t", typeof(Decimal).FullName);
            var value = XmlConvert.ToDecimal(bsonReader.ReadString("v"));
            bsonReader.ReadEndDocument();
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (Decimal) propertyMap.Getter(obj);
            bsonWriter.WriteDocumentName(propertyMap.ElementName);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", typeof(Decimal).FullName);
            bsonWriter.WriteString("v", XmlConvert.ToString(value));
            bsonWriter.WriteEndDocument();
        }
        #endregion
    }

    public class Int16PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static Int16PropertySerializer singleton = new Int16PropertySerializer();
        #endregion

        #region constructors
        private Int16PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static Int16PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(short), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            short value;
            if (bsonType == BsonType.Int32) {
                value = (short) bsonReader.ReadInt32(propertyMap.ElementName);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(short).FullName);
                value = (short) bsonReader.ReadInt32("v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.Int16");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (short) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteInt32(propertyMap.ElementName, value);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(short).FullName);
                bsonWriter.WriteInt32("v", value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class SBytePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static SBytePropertySerializer singleton = new SBytePropertySerializer();
        #endregion

        #region constructors
        private SBytePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static SBytePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(sbyte), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            sbyte value;
            if (bsonType == BsonType.Int32) {
                value = (sbyte) bsonReader.ReadInt32(propertyMap.ElementName);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(sbyte).FullName);
                value = (sbyte) bsonReader.ReadInt32("v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.SByte");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (sbyte) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteInt32(propertyMap.ElementName, value);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(sbyte).FullName);
                bsonWriter.WriteInt32("v", value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class SinglePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static SinglePropertySerializer singleton = new SinglePropertySerializer();
        #endregion

        #region constructors
        private SinglePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static SinglePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(float), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            double doubleValue;
            if (bsonType == BsonType.Double) {
                doubleValue = bsonReader.ReadDouble(propertyMap.ElementName);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(float).FullName);
                doubleValue = bsonReader.ReadDouble("v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.Single");
            }
            var value = doubleValue == double.MinValue ? float.MinValue : doubleValue == double.MaxValue ? float.MaxValue : (float) doubleValue;
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (float) propertyMap.Getter(obj);
            var doubleValue = value == float.MinValue ? double.MinValue : value == float.MaxValue ? double.MaxValue : value;
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteDouble(propertyMap.ElementName, doubleValue);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(float).FullName);
                bsonWriter.WriteDouble("v", doubleValue);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class TimeSpanPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static TimeSpanPropertySerializer singleton = new TimeSpanPropertySerializer();
        #endregion

        #region constructors
        private TimeSpanPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static TimeSpanPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(TimeSpan), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            TimeSpan value;
            if (bsonType == BsonType.Int64) {
                value = new TimeSpan(bsonReader.ReadInt64(propertyMap.ElementName));
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(TimeSpan).FullName);
                value = TimeSpan.Parse(bsonReader.ReadString("v"));
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.TimeSpan");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (TimeSpan) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteInt64(propertyMap.ElementName, value.Ticks);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(TimeSpan).FullName);
                bsonWriter.WriteString("v", value.ToString());
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class UInt16PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static UInt16PropertySerializer singleton = new UInt16PropertySerializer();
        #endregion

        #region constructors
        private UInt16PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static UInt16PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(ushort), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            ushort value;
            if (bsonType == BsonType.Int32) {
                value = (ushort) bsonReader.ReadInt32(propertyMap.ElementName);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(ushort).FullName);
                value = (ushort) bsonReader.ReadInt32("v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.UInt16");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (ushort) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteInt32(propertyMap.ElementName, value);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(ushort).FullName);
                bsonWriter.WriteInt32("v", value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class UInt32PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static UInt32PropertySerializer singleton = new UInt32PropertySerializer();
        #endregion

        #region constructors
        private UInt32PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static UInt32PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(uint), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            uint value;
            if (bsonType == BsonType.Int32) {
                value = (uint) bsonReader.ReadInt32(propertyMap.ElementName);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(uint).FullName);
                value = (uint) bsonReader.ReadInt32("v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.UInt32");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (uint) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteInt32(propertyMap.ElementName, (int) value);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(uint).FullName);
                bsonWriter.WriteInt32("v", (int) value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public class UInt64PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static UInt64PropertySerializer singleton = new UInt64PropertySerializer();
        #endregion

        #region constructors
        private UInt64PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static UInt64PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(ulong), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            ulong value;
            if (bsonType == BsonType.Int64) {
                value = (ulong) bsonReader.ReadInt64(propertyMap.ElementName);
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("_t", typeof(ulong).FullName);
                value = (ulong) bsonReader.ReadInt64("v");
                bsonReader.ReadEndDocument();
            } else {
                throw new FileFormatException("Element is not valid System.UInt64");
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (ulong) propertyMap.Getter(obj);
            if (propertyMap.UseCompactRepresentation) {
                bsonWriter.WriteInt64(propertyMap.ElementName, (long) value);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_t", typeof(ulong).FullName);
                bsonWriter.WriteInt64("v", (long) value);
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }
}
