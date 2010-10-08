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

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class BooleanPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BooleanPropertySerializer singleton = new BooleanPropertySerializer();
        #endregion

        #region constructors
        private BooleanPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BooleanPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(bool); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = bsonReader.ReadBoolean(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (bool) propertyMap.Getter(obj);
            bsonWriter.WriteBoolean(propertyMap.ElementName, value);
        }
        #endregion
    }

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

    public class DateTimePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static DateTimePropertySerializer singleton = new DateTimePropertySerializer();
        #endregion

        #region constructors
        private DateTimePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static DateTimePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(DateTime); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = bsonReader.ReadDateTime(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (DateTime) propertyMap.Getter(obj);
            bsonWriter.WriteDateTime(propertyMap.ElementName, value);
        }
        #endregion
    }

    public class DoublePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static DoublePropertySerializer singleton = new DoublePropertySerializer();
        #endregion

        #region constructors
        private DoublePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static DoublePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(double); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = bsonReader.ReadDouble(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (double) propertyMap.Getter(obj);
            bsonWriter.WriteDouble(propertyMap.ElementName, value);
        }
        #endregion
    }

    public class GuidPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static GuidPropertySerializer singleton = new GuidPropertySerializer();
        #endregion

        #region constructors
        private GuidPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static GuidPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(Guid); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            byte[] bytes;
            BsonBinarySubType subType;
            bsonReader.ReadBinaryData(propertyMap.ElementName, out bytes, out subType);
            if (bytes.Length != 16) {
                throw new FileFormatException("BinaryData length is not 16");
            }
            if (subType != BsonBinarySubType.Uuid) {
                throw new FileFormatException("BinaryData sub type is not Uuid");
            }
            var value = new Guid(bytes);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (Guid) propertyMap.Getter(obj);
            bsonWriter.WriteBinaryData(propertyMap.ElementName, value.ToByteArray(), BsonBinarySubType.Uuid);
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

    public class Int32PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static Int32PropertySerializer singleton = new Int32PropertySerializer();
        #endregion

        #region constructors
        private Int32PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static Int32PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(int); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = bsonReader.ReadInt32(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (int) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, value);
        }
        #endregion
    }

    public class Int64PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static Int64PropertySerializer singleton = new Int64PropertySerializer();
        #endregion

        #region constructors
        private Int64PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static Int64PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(long); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = bsonReader.ReadInt64(propertyMap.ElementName);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (long) propertyMap.Getter(obj);
            bsonWriter.WriteInt64(propertyMap.ElementName, value);
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

    public class StringPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static StringPropertySerializer singleton = new StringPropertySerializer();
        #endregion

        #region constructors
        private StringPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static StringPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(string); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            string value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = bsonReader.ReadString(propertyMap.ElementName);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (string) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteString(propertyMap.ElementName, value);
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
