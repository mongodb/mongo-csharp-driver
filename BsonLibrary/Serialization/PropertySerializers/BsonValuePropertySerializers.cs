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

using MongoDB.BsonLibrary;
using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class BsonBinaryDataPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonBinaryDataPropertySerializer singleton = new BsonBinaryDataPropertySerializer();
        #endregion

        #region constructors
        private BsonBinaryDataPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonBinaryDataPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonBinaryData); }
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
            var value = new BsonBinaryData(bytes, subType);
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonBinaryData) propertyMap.Getter(obj);
            bsonWriter.WriteBinaryData(propertyMap.ElementName, value.Bytes, value.SubType);
        }
        #endregion
    }

    public class BsonBooleanPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonBooleanPropertySerializer singleton = new BsonBooleanPropertySerializer();
        #endregion

        #region constructors
        private BsonBooleanPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonBooleanPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonBoolean); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = BsonBoolean.Create(bsonReader.ReadBoolean(propertyMap.ElementName));
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonBoolean) propertyMap.Getter(obj);
            bsonWriter.WriteBoolean(propertyMap.ElementName, value.Value);
        }
        #endregion
    }

    public class BsonDateTimePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonDateTimePropertySerializer singleton = new BsonDateTimePropertySerializer();
        #endregion

        #region constructors
        private BsonDateTimePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDateTimePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonDateTime); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = BsonDateTime.Create(bsonReader.ReadDateTime(propertyMap.ElementName));
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonDateTime) propertyMap.Getter(obj);
            bsonWriter.WriteDateTime(propertyMap.ElementName, value.Value);
        }
        #endregion
    }

    public class BsonDoublePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonDoublePropertySerializer singleton = new BsonDoublePropertySerializer();
        #endregion

        #region constructors
        private BsonDoublePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDoublePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonDouble); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = BsonDouble.Create(bsonReader.ReadDouble(propertyMap.ElementName));
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonDouble) propertyMap.Getter(obj);
            bsonWriter.WriteDouble(propertyMap.ElementName, value.Value);
        }
        #endregion
    }

    public class BsonInt32PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonInt32PropertySerializer singleton = new BsonInt32PropertySerializer();
        #endregion

        #region constructors
        private BsonInt32PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonInt32PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonInt32); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = BsonInt32.Create(bsonReader.ReadInt32(propertyMap.ElementName));
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonInt32) propertyMap.Getter(obj);
            bsonWriter.WriteInt32(propertyMap.ElementName, value.Value);
        }
        #endregion
    }

    public class BsonInt64PropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonInt64PropertySerializer singleton = new BsonInt64PropertySerializer();
        #endregion

        #region constructors
        private BsonInt64PropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonInt64PropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonInt64); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = BsonInt64.Create(bsonReader.ReadInt64(propertyMap.ElementName));
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonInt64) propertyMap.Getter(obj);
            bsonWriter.WriteInt64(propertyMap.ElementName, value.Value);
        }
        #endregion
    }

    public class BsonStringPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonStringPropertySerializer singleton = new BsonStringPropertySerializer();
        #endregion

        #region constructors
        private BsonStringPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonStringPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonString); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonString value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonString.Create(bsonReader.ReadString(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonString) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteString(propertyMap.ElementName, value.Value);
            }
        }
        #endregion
    }

    public class BsonSymbolPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonSymbolPropertySerializer singleton = new BsonSymbolPropertySerializer();
        #endregion

        #region constructors
        private BsonSymbolPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonSymbolPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonSymbol); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonSymbol value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonSymbol.Create(bsonReader.ReadSymbol(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonSymbol) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteSymbol(propertyMap.ElementName, value.Name);
            }
        }
        #endregion
    }
}
