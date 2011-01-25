/* Copyright 2010-2011 10gen Inc.
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
        private static BsonArraySerializer instance = new BsonArraySerializer();
        #endregion

        #region constructors
        public BsonArraySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonArraySerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonArray), instance);
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
        private static BsonBinaryDataSerializer instance = new BsonBinaryDataSerializer();
        #endregion

        #region constructors
        public BsonBinaryDataSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonBinaryDataSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonBinaryData), instance);
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
        private static BsonBooleanSerializer instance = new BsonBooleanSerializer();
        #endregion

        #region constructors
        public BsonBooleanSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonBooleanSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonBoolean), instance);
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
                return BsonBoolean.Create(BooleanSerializer.Instance.Deserialize(bsonReader, nominalType, options));
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
                BooleanSerializer.Instance.Serialize(bsonWriter, nominalType, bsonBoolean.Value, options);
            }
        }
        #endregion
    }

    public class BsonDateTimeSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDateTimeSerializer instance = new BsonDateTimeSerializer();
        #endregion

        #region constructors
        public BsonDateTimeSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDateTimeSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDateTime), instance);
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
                return BsonDateTime.Create(DateTimeSerializer.Instance.Deserialize(bsonReader, nominalType, options));
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
                DateTimeSerializer.Instance.Serialize(bsonWriter, nominalType, bsonDateTime.Value, options);
            }
        }
        #endregion
    }

    public class BsonDocumentSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDocumentSerializer instance = new BsonDocumentSerializer();
        #endregion

        #region constructors
        public BsonDocumentSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDocumentSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDocument), instance);
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
        private static BsonDocumentWrapperSerializer instance = new BsonDocumentWrapperSerializer();
        #endregion

        #region constructors
        public BsonDocumentWrapperSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDocumentWrapperSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDocumentWrapper), instance);
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
        private static BsonDoubleSerializer instance = new BsonDoubleSerializer();
        #endregion

        #region constructors
        public BsonDoubleSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDoubleSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonDouble), instance);
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
                return BsonDouble.Create(DoubleSerializer.Instance.Deserialize(bsonReader, nominalType, options));
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
                DoubleSerializer.Instance.Serialize(bsonWriter, nominalType, bsonDouble.Value, options);
            }
        }
        #endregion
    }

    public class BsonInt32Serializer : BsonBaseSerializer {
        #region private static fields
        private static BsonInt32Serializer instance = new BsonInt32Serializer();
        #endregion

        #region constructors
        public BsonInt32Serializer() {
        }
        #endregion

        #region public static properties
        public static BsonInt32Serializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonInt32), instance);
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
                return BsonInt32.Create(Int32Serializer.Instance.Deserialize(bsonReader, nominalType, options));
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
                Int32Serializer.Instance.Serialize(bsonWriter, nominalType, bsonInt32.Value, options);
            }
        }
        #endregion
    }

    public class BsonInt64Serializer : BsonBaseSerializer {
        #region private static fields
        private static BsonInt64Serializer instance = new BsonInt64Serializer();
        #endregion

        #region constructors
        public BsonInt64Serializer() {
        }
        #endregion

        #region public static properties
        public static BsonInt64Serializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonInt64), instance);
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
                return BsonInt64.Create(Int64Serializer.Instance.Deserialize(bsonReader, nominalType, options));
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
                Int64Serializer.Instance.Serialize(bsonWriter, nominalType, bsonInt64.Value, options);
            }
        }
        #endregion
    }

    public class BsonJavaScriptSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonJavaScriptSerializer instance = new BsonJavaScriptSerializer();
        #endregion

        #region constructors
        public BsonJavaScriptSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonJavaScriptSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonJavaScript), instance);
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
        private static BsonJavaScriptWithScopeSerializer instance = new BsonJavaScriptWithScopeSerializer();
        #endregion

        #region constructors
        public BsonJavaScriptWithScopeSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonJavaScriptWithScopeSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonJavaScriptWithScope), instance);
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
        private static BsonMaxKeySerializer instance = new BsonMaxKeySerializer();
        #endregion

        #region constructors
        public BsonMaxKeySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonMaxKeySerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonMaxKey), instance);
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
        private static BsonMinKeySerializer instance = new BsonMinKeySerializer();
        #endregion

        #region constructors
        public BsonMinKeySerializer() {
        }
        #endregion

        #region public static properties
        public static BsonMinKeySerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonMinKey), instance);
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
        private static BsonNullSerializer instance = new BsonNullSerializer();
        #endregion

        #region constructors
        public BsonNullSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonNullSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonNull), instance);
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
        private static BsonObjectIdSerializer instance = new BsonObjectIdSerializer();
        #endregion

        #region constructors
        public BsonObjectIdSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonObjectIdSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonObjectId), instance);
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
                return BsonObjectId.Create(ObjectIdSerializer.Instance.Deserialize(bsonReader, nominalType, options));
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
                ObjectIdSerializer.Instance.Serialize(bsonWriter, nominalType, bsonObjectId.Value, options);
            }
        }
        #endregion
    }

    public class BsonRegularExpressionSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonRegularExpressionSerializer instance = new BsonRegularExpressionSerializer();
        #endregion

        #region constructors
        public BsonRegularExpressionSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonRegularExpressionSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonRegularExpression), instance);
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
        private static BsonStringSerializer instance = new BsonStringSerializer();
        #endregion

        #region constructors
        public BsonStringSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonStringSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonString), instance);
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
                return BsonString.Create(StringSerializer.Instance.Deserialize(bsonReader, nominalType, options));
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
                StringSerializer.Instance.Serialize(bsonWriter, nominalType, bsonString.Value, options);
            }
        }
        #endregion
    }

    public class BsonSymbolSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonSymbolSerializer instance = new BsonSymbolSerializer();
        #endregion

        #region constructors
        public BsonSymbolSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonSymbolSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonSymbol), instance);
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
                case BsonType.String:
                    return BsonSymbol.Create(bsonReader.ReadString());
                case BsonType.Symbol:
                    return BsonSymbol.Create(bsonReader.ReadSymbol());
                default:
                    var message = string.Format("Cannot deserialize BsonSymbol from BsonType: {0}", bsonType);
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
                var symbol = (BsonSymbol) value;
                var representation = (options == null) ? BsonType.Symbol : ((RepresentationSerializationOptions) options).Representation;
                switch (representation) {
                    case BsonType.String:
                        bsonWriter.WriteString(symbol.Name);
                        break;
                    case BsonType.Symbol:
                        bsonWriter.WriteSymbol(symbol.Name);
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid representation for type 'BsonSymbol'", representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
        #endregion
    }

    public class BsonTimestampSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonTimestampSerializer instance = new BsonTimestampSerializer();
        #endregion

        #region constructors
        public BsonTimestampSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonTimestampSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonTimestamp), instance);
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
        private static BsonValueSerializer instance = new BsonValueSerializer();
        #endregion

        #region constructors
        public BsonValueSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonValueSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(BsonValue), instance);
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
