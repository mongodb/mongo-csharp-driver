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

        #region public properties
        public Type PropertyType {
            get { return typeof(byte); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (byte) bsonReader.ReadInt32(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (byte) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, value);
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

        #region public properties
        public Type PropertyType {
            get { return typeof(char); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (char) bsonReader.ReadInt32(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (char) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, value);
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

        #region public properties
        public Type PropertyType {
            get { return typeof(DateTimeOffset); }
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
            bsonReader.VerifyString("_t", typeof(DateTimeOffset).FullName);
            var dateTime = DateTime.Parse(bsonReader.ReadString("dt")); // Kind = DateTimeKind.Unspecified
            var offset = TimeSpan.Parse(bsonReader.ReadString("o"));
            bsonReader.ReadEndDocument();
            var value = new DateTimeOffset(dateTime, offset);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            // note: the DateTime portion has to be serialized as a string because it is NOT in UTC
            var value = (DateTimeOffset) propertyMap.Getter(obj);
            bsonWriter.WriteDocumentName(propertyMap.ElementName);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", typeof(DateTimeOffset).FullName);
            bsonWriter.WriteString("dt", value.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF")); // omit trailing zeros
            bsonWriter.WriteString("o", Regex.Replace(value.Offset.ToString(), ":00$", "")); // omit trailing zeros
            bsonWriter.WriteEndDocument();
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

        #region public properties
        public Type PropertyType {
            get { return typeof(Decimal); }
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

        #region public properties
        public Type PropertyType {
            get { return typeof(short); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (short) bsonReader.ReadInt32(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (short) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, value);
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

        #region public properties
        public Type PropertyType {
            get { return typeof(sbyte); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (sbyte) bsonReader.ReadInt32(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (sbyte) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, value);
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

        #region public properties
        public Type PropertyType {
            get { return typeof(float); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (float) bsonReader.ReadDouble(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (float) propertyMap.Getter(obj);
            bsonWriter.WriteDouble(propertyMap.ElementName, value);
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

        #region public properties
        public Type PropertyType {
            get { return typeof(TimeSpan); }
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
            bsonReader.VerifyString("_t", typeof(TimeSpan).FullName);
            var ticks = bsonReader.ReadInt64("v");
            bsonReader.ReadEndDocument();
            var value = new TimeSpan(ticks);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            // note: the DateTime portion has to be serialized as a string because it is NOT in UTC
            var value = (TimeSpan) propertyMap.Getter(obj);
            bsonWriter.WriteDocumentName(propertyMap.ElementName);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", typeof(TimeSpan).FullName);
            bsonWriter.WriteInt64("v", value.Ticks);
            bsonWriter.WriteEndDocument();
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

        #region public properties
        public Type PropertyType {
            get { return typeof(ushort); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (ushort) bsonReader.ReadInt32(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (ushort) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, (int) value);
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

        #region public properties
        public Type PropertyType {
            get { return typeof(uint); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (uint) bsonReader.ReadInt32(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (uint) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, (int) value);
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

        #region public properties
        public Type PropertyType {
            get { return typeof(ulong); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (ulong) bsonReader.ReadInt64(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (ulong) propertyMap.Getter(obj);
            bsonWriter.WriteInt64(propertyMap.ElementName, (long) value);
        }
        #endregion
    }
}
