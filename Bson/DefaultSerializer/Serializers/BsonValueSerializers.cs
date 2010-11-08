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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonArray.ReadFrom(bsonReader);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData(out bytes, out subType);
                return new BsonBinaryData(bytes, subType);
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
                var binaryData = (BsonBinaryData) value;
                bsonWriter.WriteBinaryData(binaryData.Bytes, binaryData.SubType);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonBoolean.Create(bsonReader.ReadBoolean());
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
                bsonWriter.WriteBoolean(((BsonBoolean) value).Value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonDateTime.Create(bsonReader.ReadDateTime());
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
                bsonWriter.WriteDateTime(((BsonDateTime) value).Value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            return BsonDocument.ReadFrom(bsonReader);
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

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                ((BsonDocument) value).Serialize(bsonWriter, nominalType, serializeIdFirst);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException();
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
                ((BsonDocumentWrapper) value).Serialize(bsonWriter, typeof(BsonDocument), serializeIdFirst);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonDouble.Create(bsonReader.ReadDouble());
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
                bsonWriter.WriteDouble(((BsonDouble) value).Value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonInt32.Create(bsonReader.ReadInt32());
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
                bsonWriter.WriteInt32(((BsonInt32) value).Value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonInt64.Create(bsonReader.ReadInt64());
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
                bsonWriter.WriteInt64(((BsonInt64) value).Value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return new BsonJavaScript(bsonReader.ReadJavaScript());
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
                bsonWriter.WriteJavaScript(((BsonJavaScript) value).Code);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var code = bsonReader.ReadJavaScriptWithScope();
                var scope = BsonDocument.ReadFrom(bsonReader);
                return new BsonJavaScriptWithScope(code, scope);
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
                var script = (BsonJavaScriptWithScope) value;
                bsonWriter.WriteJavaScriptWithScope(script.Code);
                script.Scope.WriteTo(bsonWriter);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadMaxKey();
                return BsonMaxKey.Value;
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
                bsonWriter.WriteMaxKey();
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadMinKey();
                return BsonMinKey.Value;
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
                bsonWriter.WriteMinKey();
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return BsonNull.Value;
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadStartDocument();
                var csharpNull = bsonReader.ReadBoolean("$csharpnull");
                bsonReader.ReadEndDocument();
                return csharpNull ? null : BsonNull.Value;
            } 
            throw new FileFormatException("Invalid representation for BsonNull");
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            if (value == null) {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteBoolean("$csharpnull", true);
                bsonWriter.WriteEndDocument();
            } else {
                bsonWriter.WriteNull();
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                int timestamp;
                long machinePidIncrement;
                bsonReader.ReadObjectId(out timestamp, out machinePidIncrement);
                return new BsonObjectId(timestamp, machinePidIncrement);
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
                var objectId = (BsonObjectId) value;
                bsonWriter.WriteObjectId(objectId.Timestamp, objectId.MachinePidIncrement);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                string pattern, options;
                bsonReader.ReadRegularExpression(out pattern, out options);
                return new BsonRegularExpression(pattern, options);
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
                var regex = (BsonRegularExpression) value;
                bsonWriter.WriteRegularExpression(regex.Pattern, regex.Options);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonString.Create(bsonReader.ReadString());
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
                bsonWriter.WriteString(((BsonString) value).Value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonSymbol.Create(bsonReader.ReadSymbol());
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
                bsonWriter.WriteSymbol(((BsonSymbol) value).Name);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonTimestamp.Create(bsonReader.ReadTimestamp());
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
                bsonWriter.WriteTimestamp(((BsonTimestamp) value).Value);
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
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonValue.ReadFrom(bsonReader);
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
                ((BsonValue) value).WriteTo(bsonWriter);
            }
        }
        #endregion
    }
}
