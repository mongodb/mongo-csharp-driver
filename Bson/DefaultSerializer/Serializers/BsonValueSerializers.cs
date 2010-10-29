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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class BsonArraySerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonArraySerializer singleton = new BsonArraySerializer();
        #endregion

        #region constructors
        private BsonArraySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonArraySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonArray), singleton);
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
            } else {
                bsonReader.ReadArrayName(out name);
                return BsonArray.ReadFrom(bsonReader);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteArrayName(name);
                ((BsonArray) value).WriteTo(bsonWriter);
            }
        }
        #endregion
    }

    public class BsonBinaryDataSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonBinaryDataSerializer singleton = new BsonBinaryDataSerializer();
        #endregion

        #region constructors
        private BsonBinaryDataSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonBinaryDataSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonBinaryData), singleton);
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
            } else {
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData(out name, out bytes, out subType);
                return new BsonBinaryData(bytes, subType);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonBinaryData) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteBinaryData(name, value.Bytes, value.SubType);
            }
        }
        #endregion
    }

    public class BsonBooleanSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonBooleanSerializer singleton = new BsonBooleanSerializer();
        #endregion

        #region constructors
        private BsonBooleanSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonBooleanSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonBoolean), singleton);
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
            } else {
                return BsonBoolean.Create(bsonReader.ReadBoolean(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonBoolean) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteBoolean(name, value.Value);
            }
        }
        #endregion
    }

    public class BsonDateTimeSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDateTimeSerializer singleton = new BsonDateTimeSerializer();
        #endregion

        #region constructors
        private BsonDateTimeSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDateTimeSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDateTime), singleton);
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
            } else {
                return BsonDateTime.Create(bsonReader.ReadDateTime(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonDateTime) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteDateTime(name, value.Value);
            }
        }
        #endregion
    }

    public class BsonDocumentSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDocumentSerializer singleton = new BsonDocumentSerializer();
        #endregion

        #region constructors
        private BsonDocumentSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDocumentSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDocument), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            return BsonDocument.ReadFrom(bsonReader);
        }

        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else {
                bsonReader.ReadDocumentName(out name);
                return BsonDocument.ReadFrom(bsonReader);
            }
        }

        public override bool DocumentHasIdMember(
            object document
        ) {
            var bsonDocument = (BsonDocument) document;
            return bsonDocument.DocumentHasIdMember();
        }

        public override bool DocumentHasIdValue(
            object document,
            out object existingId
        ) {
            var bsonDocument = (BsonDocument) document;
            return bsonDocument.DocumentHasIdValue(out existingId);
        }

        public override void GenerateDocumentId(
            object document
        ) {
            var bsonDocument = (BsonDocument) document;
            bsonDocument.GenerateDocumentId();
        }

        public override void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            var value = (BsonDocument) document;
            value.SerializeDocument(bsonWriter, nominalType, serializeIdFirst);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonDocument) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                value.SerializeElement(bsonWriter, nominalType, name);
            }
        }
        #endregion
    }

    public class BsonDocumentWrapperSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDocumentWrapperSerializer singleton = new BsonDocumentWrapperSerializer();
        #endregion

        #region constructors
        private BsonDocumentWrapperSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDocumentWrapperSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDocumentWrapper), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            throw new InvalidOperationException();
        }

        public override void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            var value = (BsonDocumentWrapper) document;
            value.SerializeDocument(bsonWriter, nominalType, serializeIdFirst);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonDocumentWrapper) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteDocumentName(name);
                value.SerializeDocument(bsonWriter, typeof(BsonDocument), false);
            }
        }
        #endregion
    }

    public class BsonDoubleSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDoubleSerializer singleton = new BsonDoubleSerializer();
        #endregion

        #region constructors
        private BsonDoubleSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDoubleSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDouble), singleton);
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
            } else {
                return BsonDouble.Create(bsonReader.ReadDouble(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonDouble) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteDouble(name, value.Value);
            }
        }
        #endregion
    }

    public class BsonInt32Serializer : BsonBaseSerializer {
        #region private static fields
        private static BsonInt32Serializer singleton = new BsonInt32Serializer();
        #endregion

        #region constructors
        private BsonInt32Serializer() {
        }
        #endregion

        #region public static properties
        public static BsonInt32Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonInt32), singleton);
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
            } else {
                return BsonInt32.Create(bsonReader.ReadInt32(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonInt32) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteInt32(name, value.Value);
            }
        }
        #endregion
    }

    public class BsonInt64Serializer : BsonBaseSerializer {
        #region private static fields
        private static BsonInt64Serializer singleton = new BsonInt64Serializer();
        #endregion

        #region constructors
        private BsonInt64Serializer() {
        }
        #endregion

        #region public static properties
        public static BsonInt64Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonInt64), singleton);
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
            } else {
                return BsonInt64.Create(bsonReader.ReadInt64(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonInt64) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteInt64(name, value.Value);
            }
        }
        #endregion
    }

    public class BsonJavaScriptSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonJavaScriptSerializer singleton = new BsonJavaScriptSerializer();
        #endregion

        #region constructors
        private BsonJavaScriptSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonJavaScriptSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonJavaScript), singleton);
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
            } else {
                return new BsonJavaScript(bsonReader.ReadJavaScript(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonJavaScript) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteJavaScript(name, value.Code);
            }
        }
        #endregion
    }

    public class BsonJavaScriptWithScopeSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonJavaScriptWithScopeSerializer singleton = new BsonJavaScriptWithScopeSerializer();
        #endregion

        #region constructors
        private BsonJavaScriptWithScopeSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonJavaScriptWithScopeSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonJavaScriptWithScope), singleton);
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
            } else {
                var code = bsonReader.ReadJavaScriptWithScope(out name);
                var scope = BsonDocument.ReadFrom(bsonReader);
                return new BsonJavaScriptWithScope(code, scope);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonJavaScriptWithScope) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteJavaScriptWithScope(name, value.Code);
                value.Scope.WriteTo(bsonWriter);
            }
        }
        #endregion
    }

    public class BsonMaxKeySerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonMaxKeySerializer singleton = new BsonMaxKeySerializer();
        #endregion

        #region constructors
        private BsonMaxKeySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonMaxKeySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonMaxKey), singleton);
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
            } else {
                bsonReader.ReadMaxKey(out name);
                return BsonConstants.MaxKey;
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonMaxKey) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteMaxKey(name);
            }
        }
        #endregion
    }

    public class BsonMinKeySerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonMinKeySerializer singleton = new BsonMinKeySerializer();
        #endregion

        #region constructors
        private BsonMinKeySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonMinKeySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonMinKey), singleton);
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
            } else {
                bsonReader.ReadMinKey(out name);
                return BsonConstants.MinKey;
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonMinKey) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteMinKey(name);
            }
        }
        #endregion
    }

    public class BsonNullSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonNullSerializer singleton = new BsonNullSerializer();
        #endregion

        #region constructors
        private BsonNullSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonNullSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonNull), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Document) {
                bsonReader.ReadDocumentName(out name);
                bsonReader.ReadStartDocument();
                bsonReader.SkipElement("$csharpnull");
                bsonReader.ReadEndDocument();
                return null;
            } else {
                bsonReader.ReadNull(out name);
                return BsonConstants.Null;
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonNull) obj;
            if (value == null) {
                bsonWriter.WriteDocumentName(name);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteBoolean("$csharpnull", true);
                bsonWriter.WriteEndDocument();
            } else {
                bsonWriter.WriteNull(name);
            }
        }
        #endregion
    }

    public class BsonObjectIdSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonObjectIdSerializer singleton = new BsonObjectIdSerializer();
        #endregion

        #region constructors
        private BsonObjectIdSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonObjectIdSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonObjectId), singleton);
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
            } else {
                int timestamp;
                long machinePidIncrement;
                bsonReader.ReadObjectId(out name, out timestamp, out machinePidIncrement);
                return new BsonObjectId(timestamp, machinePidIncrement);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonObjectId) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteObjectId(name, value.Timestamp, value.MachinePidIncrement);
            }
        }
        #endregion
    }

    public class BsonRegularExpressionSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonRegularExpressionSerializer singleton = new BsonRegularExpressionSerializer();
        #endregion

        #region constructors
        private BsonRegularExpressionSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonRegularExpressionSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonRegularExpression), singleton);
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
            } else {
                string pattern, options;
                bsonReader.ReadRegularExpression(out name, out pattern, out options);
                return new BsonRegularExpression(pattern, options);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonRegularExpression) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteRegularExpression(name, value.Pattern, value.Options);
            }
        }
        #endregion
    }

    public class BsonStringSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonStringSerializer singleton = new BsonStringSerializer();
        #endregion

        #region constructors
        private BsonStringSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonStringSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonString), singleton);
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
            } else {
                return BsonString.Create(bsonReader.ReadString(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonString) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteString(name, value.Value);
            }
        }
        #endregion
    }

    public class BsonSymbolSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonSymbolSerializer singleton = new BsonSymbolSerializer();
        #endregion

        #region constructors
        private BsonSymbolSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonSymbolSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonSymbol), singleton);
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
            } else {
                return BsonSymbol.Create(bsonReader.ReadSymbol(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonSymbol) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteSymbol(name, value.Name);
            }
        }
        #endregion
    }

    public class BsonTimestampSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonTimestampSerializer singleton = new BsonTimestampSerializer();
        #endregion

        #region constructors
        private BsonTimestampSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonTimestampSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonTimestamp), singleton);
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
            } else {
                return BsonTimestamp.Create(bsonReader.ReadTimestamp(out name));
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonTimestamp) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteTimestamp(name, value.Value);
            }
        }
        #endregion
    }

    public class BsonValueSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonValueSerializer singleton = new BsonValueSerializer();
        #endregion

        #region constructors
        private BsonValueSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonValueSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonValue), singleton);
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
            } else {
                BsonElement element;
                BsonElement.ReadFrom(bsonReader, out element);
                name = element.Name;
                return element.Value;
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj
        ) {
            var value = (BsonValue) obj;
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                var element = new BsonElement(name, value);
                element.WriteTo(bsonWriter);
            }
        }
        #endregion
    }
}
