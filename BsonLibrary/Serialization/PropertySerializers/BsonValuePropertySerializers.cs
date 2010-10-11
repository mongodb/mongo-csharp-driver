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
    public class BsonArrayPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonArrayPropertySerializer singleton = new BsonArrayPropertySerializer();
        #endregion

        #region constructors
        private BsonArrayPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonArrayPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonArray); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonArray value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                bsonReader.ReadArrayName(propertyMap.ElementName);
                value = BsonArray.ReadFrom(bsonReader);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonArray) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteArrayName(propertyMap.ElementName);
                value.WriteTo(bsonWriter);
            }
        }
        #endregion
    }

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
            var bsonType = bsonReader.PeekBsonType();
            BsonBinaryData value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData(propertyMap.ElementName, out bytes, out subType);
                value = new BsonBinaryData(bytes, subType);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonBinaryData) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteBinaryData(propertyMap.ElementName, value.Bytes, value.SubType);
            }
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
            var bsonType = bsonReader.PeekBsonType();
            BsonBoolean value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonBoolean.Create(bsonReader.ReadBoolean(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonBoolean) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteBoolean(propertyMap.ElementName, value.Value);
            }
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
            var bsonType = bsonReader.PeekBsonType();
            BsonDateTime value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonDateTime.Create(bsonReader.ReadDateTime(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonDateTime) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteDateTime(propertyMap.ElementName, value.Value);
            }
        }
        #endregion
    }

    public class BsonDocumentPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonDocumentPropertySerializer singleton = new BsonDocumentPropertySerializer();
        #endregion

        #region constructors
        private BsonDocumentPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDocumentPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonDocument); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonDocument value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                value = BsonDocument.ReadFrom(bsonReader);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonDocument) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                value.WriteTo(bsonWriter);
            }
        }
        #endregion
    }

    public class BsonDocumentWrapperPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonDocumentWrapperPropertySerializer singleton = new BsonDocumentWrapperPropertySerializer();
        #endregion

        #region constructors
        private BsonDocumentWrapperPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDocumentWrapperPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonDocumentWrapper); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            throw new InvalidOperationException("BsonDocumentWrappers cannot be deserialized");
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonDocumentWrapper) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                value.Serialize(bsonWriter);
            }
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
            var bsonType = bsonReader.PeekBsonType();
            BsonDouble value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonDouble.Create(bsonReader.ReadDouble(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonDouble) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteDouble(propertyMap.ElementName, value.Value);
            }
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
            var bsonType = bsonReader.PeekBsonType();
            BsonInt32 value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonInt32.Create(bsonReader.ReadInt32(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonInt32) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteInt32(propertyMap.ElementName, value.Value);
            }
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
            var bsonType = bsonReader.PeekBsonType();
            BsonInt64 value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonInt64.Create(bsonReader.ReadInt64(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonInt64) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteInt64(propertyMap.ElementName, value.Value);
            }
        }
        #endregion
    }

    public class BsonJavaScriptPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonJavaScriptPropertySerializer singleton = new BsonJavaScriptPropertySerializer();
        #endregion

        #region constructors
        private BsonJavaScriptPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonJavaScriptPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonJavaScript); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonJavaScript value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = new BsonJavaScript(bsonReader.ReadJavaScript(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonJavaScript) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteJavaScript(propertyMap.ElementName, value.Code);
            }
        }
        #endregion
    }

    public class BsonJavaScriptWithScopePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonJavaScriptWithScopePropertySerializer singleton = new BsonJavaScriptWithScopePropertySerializer();
        #endregion

        #region constructors
        private BsonJavaScriptWithScopePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonJavaScriptWithScopePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonJavaScriptWithScope); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonJavaScriptWithScope value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                var code = bsonReader.ReadJavaScriptWithScope(propertyMap.ElementName);
                var scope = BsonDocument.ReadFrom(bsonReader);
                value = new BsonJavaScriptWithScope(code, scope);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonJavaScriptWithScope) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteJavaScriptWithScope(propertyMap.ElementName, value.Code);
                value.Scope.WriteTo(bsonWriter);
            }
        }
        #endregion
    }

    public class BsonMaxKeyPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonMaxKeyPropertySerializer singleton = new BsonMaxKeyPropertySerializer();
        #endregion

        #region constructors
        private BsonMaxKeyPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonMaxKeyPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonMaxKey); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonMaxKey value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                bsonReader.ReadMaxKey(propertyMap.ElementName);
                value = Bson.MaxKey;
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonMaxKey) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteMaxKey(propertyMap.ElementName);
            }
        }
        #endregion
    }

    public class BsonMinKeyPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonMinKeyPropertySerializer singleton = new BsonMinKeyPropertySerializer();
        #endregion

        #region constructors
        private BsonMinKeyPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonMinKeyPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonMinKey); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonMinKey value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                bsonReader.ReadMinKey(propertyMap.ElementName);
                value = Bson.MinKey;
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonMinKey) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteMinKey(propertyMap.ElementName);
            }
        }
        #endregion
    }

    public class BsonNullPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonNullPropertySerializer singleton = new BsonNullPropertySerializer();
        #endregion

        #region constructors
        private BsonNullPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonNullPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonNull); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonNull value;
            if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(propertyMap.ElementName);
                bsonReader.ReadStartDocument();
                bsonReader.VerifyString("$null", "c#null");
                bsonReader.ReadEndDocument();
                value = null;
            } else {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = Bson.Null;
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonNull) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteDocumentName(propertyMap.ElementName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("$null", "c#null");
                bsonWriter.WriteEndDocument();
            } else {
                bsonWriter.WriteNull(propertyMap.ElementName);
            }
        }
        #endregion
    }

    public class BsonObjectIdPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonObjectIdPropertySerializer singleton = new BsonObjectIdPropertySerializer();
        #endregion

        #region constructors
        private BsonObjectIdPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonObjectIdPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonObjectId); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonObjectId value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                int timestamp;
                long machinePidIncrement;
                bsonReader.ReadObjectId(propertyMap.ElementName, out timestamp, out machinePidIncrement);
                value = new BsonObjectId(timestamp, machinePidIncrement);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonObjectId) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteObjectId(propertyMap.ElementName, value.Timestamp, value.MachinePidIncrement);
            }
        }
        #endregion
    }

    public class BsonRegularExpressionPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonRegularExpressionPropertySerializer singleton = new BsonRegularExpressionPropertySerializer();
        #endregion

        #region constructors
        private BsonRegularExpressionPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonRegularExpressionPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonRegularExpression); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonRegularExpression value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                string pattern, options;
                bsonReader.ReadRegularExpression(propertyMap.ElementName, out pattern, out options);
                value = new BsonRegularExpression(pattern, options);
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonRegularExpression) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteRegularExpression(propertyMap.ElementName, value.Pattern, value.Options);
            }
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

    public class BsonTimestampPropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonTimestampPropertySerializer singleton = new BsonTimestampPropertySerializer();
        #endregion

        #region constructors
        private BsonTimestampPropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonTimestampPropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonTimestamp); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonTimestamp value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                value = BsonTimestamp.Create(bsonReader.ReadTimestamp(propertyMap.ElementName));
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonTimestamp) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                bsonWriter.WriteTimestamp(propertyMap.ElementName, value.Value);
            }
        }
        #endregion
    }

    public class BsonValuePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static BsonValuePropertySerializer singleton = new BsonValuePropertySerializer();
        #endregion

        #region constructors
        private BsonValuePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonValuePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public properties
        public Type PropertyType {
            get { return typeof(BsonValue); }
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var bsonType = bsonReader.PeekBsonType();
            BsonValue value;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(propertyMap.ElementName);
                value = null;
            } else {
                BsonElement element = BsonElement.ReadFrom(bsonReader, propertyMap.ElementName);
                value = element.Value;
            }
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (BsonValue) propertyMap.Getter(obj);
            if (value == null) {
                bsonWriter.WriteNull(propertyMap.ElementName);
            } else {
                var element = new BsonElement(propertyMap.ElementName, value);
                element.WriteTo(bsonWriter);
            }
        }
        #endregion
    }
}
