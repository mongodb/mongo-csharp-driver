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
            Type nominalType,
            IBsonSerializationOptions options
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
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var array = (BsonArray) value;
                array.WriteTo(bsonWriter);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    return new BsonBinaryData(bytes, subType);
                default:
                    var message = string.Format("Cannot deserialize BsonBinaryData from BsonType: {0}", bsonType);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonBoolean.Create(BooleanSerializer.Singleton.Deserialize(bsonReader, nominalType, options));
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
                var bsonBoolean = (BsonBoolean) value;
                BooleanSerializer.Singleton.Serialize(bsonWriter, nominalType, bsonBoolean.Value, options);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonDateTime.Create(DateTimeSerializer.Singleton.Deserialize(bsonReader, nominalType, options));
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
                var bsonDateTime = (BsonDateTime) value;
                DateTimeSerializer.Singleton.Serialize(bsonWriter, nominalType, bsonDateTime.Value, options);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            return BsonDocument.ReadFrom(bsonReader);
        }

        public override bool GetDocumentId(
            object document,
            out object id,
            out IIdGenerator idGenerator
        ) {
            var bsonDocument = (BsonDocument) document;
            return bsonDocument.GetDocumentId(out id, out idGenerator);
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
                var document = (BsonDocument) value;
                document.Serialize(bsonWriter, nominalType, options);
            }
        }

        public override void SetDocumentId(
            object document,
            object id
        ) {
            var bsonDocument = (BsonDocument) document;
            bsonDocument.SetDocumentId(id);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            throw new InvalidOperationException();
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
                var documentWrapper = (BsonDocumentWrapper) value;
                documentWrapper.Serialize(bsonWriter, nominalType, options);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonDouble.Create(DoubleSerializer.Singleton.Deserialize(bsonReader, nominalType, options));
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
                var bsonDouble = (BsonDouble) value;
                DoubleSerializer.Singleton.Serialize(bsonWriter, nominalType, bsonDouble.Value, options);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonInt32.Create(Int32Serializer.Singleton.Deserialize(bsonReader, nominalType, options));
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
                var bsonInt32 = (BsonInt32) value;
                Int32Serializer.Singleton.Serialize(bsonWriter, nominalType, bsonInt32.Value, options);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonInt64.Create(Int64Serializer.Singleton.Deserialize(bsonReader, nominalType, options));
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
                var bsonInt64 = (BsonInt64) value;
                Int64Serializer.Singleton.Serialize(bsonWriter, nominalType, bsonInt64.Value, options);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var code = bsonReader.ReadJavaScript();
                return new BsonJavaScript(code);
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
                var script = (BsonJavaScript) value;
                bsonWriter.WriteJavaScript(script.Code);
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
            Type nominalType,
            IBsonSerializationOptions options
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
            IBsonSerializationOptions options
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
            Type nominalType,
            IBsonSerializationOptions options
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
            IBsonSerializationOptions options
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
            Type nominalType,
            IBsonSerializationOptions options
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
            IBsonSerializationOptions options
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return BsonNull.Value;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var csharpNull = bsonReader.ReadBoolean("$csharpnull");
                    bsonReader.ReadEndDocument();
                    return csharpNull ? null : BsonNull.Value;
                default:
                    var message = string.Format("Cannot deserialize BsonNull from BsonType: {0}", bsonType);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonObjectId.Create(ObjectIdSerializer.Singleton.Deserialize(bsonReader, nominalType, options));
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
                var bsonObjectId = (BsonObjectId) value;
                ObjectIdSerializer.Singleton.Serialize(bsonWriter, nominalType, bsonObjectId.Value, options);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                string regexPattern, regexOptions;
                bsonReader.ReadRegularExpression(out regexPattern, out regexOptions);
                return new BsonRegularExpression(regexPattern, regexOptions);
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
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonString.Create(StringSerializer.Singleton.Deserialize(bsonReader, nominalType, options));
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
                var bsonString = (BsonString) value;
                StringSerializer.Singleton.Serialize(bsonWriter, nominalType, bsonString.Value, options);
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
            Type nominalType,
            IBsonSerializationOptions options
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
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var symbol = (BsonSymbol) value;
                bsonWriter.WriteSymbol(symbol.Name);
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
            Type nominalType,
            IBsonSerializationOptions options
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
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var timestamp = (BsonTimestamp) value;
                bsonWriter.WriteTimestamp(timestamp.Value);
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
            Type nominalType,
            IBsonSerializationOptions options
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
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonValue = (BsonValue) value;
                bsonValue.WriteTo(bsonWriter);
            }
        }
        #endregion
    }
}
