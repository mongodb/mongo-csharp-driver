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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MongoDB.Bson.IO;

namespace MongoDB.Bson {
    [Serializable]
    public abstract class BsonValue : IComparable<BsonValue>, IConvertible, IEquatable<BsonValue> {
        #region private static fields
        private static Dictionary<BsonType, int> bsonTypeSortOrder = new Dictionary<BsonType, int> {
            { BsonType.MinKey, 1 },
            { BsonType.Null, 2 },
            { BsonType.Double, 3 },
            { BsonType.Int32, 3 },
            { BsonType.Int64, 3 },
            { BsonType.String, 4 },
            { BsonType.Symbol, 4 },
            { BsonType.Document, 5 },
            { BsonType.Array, 6 },
            { BsonType.Binary, 7 },
            { BsonType.ObjectId, 8 },
            { BsonType.Boolean, 9 },
            { BsonType.DateTime, 10 },
            { BsonType.Timestamp, 10 },
            { BsonType.RegularExpression, 11 },
            { BsonType.JavaScript, 12 }, // TODO: confirm where JavaScript and JavaScriptWithScope are in the sort order
            { BsonType.JavaScriptWithScope, 13 },
            { BsonType.MaxKey, 14 },
        };
        #endregion

        #region protected fields
        protected BsonType bsonType;
        #endregion

        #region constructors
        protected BsonValue(
            BsonType bsonType
        ) {
            this.bsonType = bsonType;
        }
        #endregion

        #region public properties
        public bool AsBoolean {
            get { return ((BsonBoolean) this).Value; }
        }

        public BsonArray AsBsonArray {
            get { return (BsonArray) this; }
        }

        public BsonBinaryData AsBsonBinaryData {
            get { return (BsonBinaryData) this; }
        }

        public BsonDocument AsBsonDocument {
            get { return (BsonDocument) this; }
        }

        public BsonJavaScript AsBsonJavaScript {
            get { return (BsonJavaScript) this; }
        }

        public BsonJavaScriptWithScope AsBsonJavaScriptWithScope {
            get { return (BsonJavaScriptWithScope) this; }
        }

        public BsonMaxKey AsBsonMaxKey {
            get { return (BsonMaxKey) this; }
        }

        public BsonMinKey AsBsonMinKey {
            get { return (BsonMinKey) this; }
        }

        public BsonNull AsBsonNull {
            get { return (BsonNull) this; }
        }

        public BsonRegularExpression AsBsonRegularExpression {
            get { return (BsonRegularExpression) this; }
        }

        public BsonSymbol AsBsonSymbol {
            get { return (BsonSymbol) this; }
        }

        public BsonTimestamp AsBsonTimestamp {
            get { return (BsonTimestamp) this; }
        }

        public byte[] AsByteArray {
            get { return ((BsonBinaryData) this).Bytes; }
        }

        public DateTime AsDateTime {
            get { return ((BsonDateTime) this).Value; }
        }

        public double AsDouble {
            get { return ((BsonDouble) this).Value; }
        }

        public Guid AsGuid {
            get { return ((BsonBinaryData) this).ToGuid(); }
        }

        public int AsInt32 {
            get { return ((BsonInt32) this).Value; }
        }

        public long AsInt64 {
            get { return ((BsonInt64) this).Value; }
        }

        public bool? AsNullableBoolean {
            get { return (bsonType == BsonType.Null) ? null : (bool?) AsBoolean; }
        }

        public DateTime? AsNullableDateTime {
            get { return (bsonType == BsonType.Null) ? null : (DateTime?) AsDateTime; }
        }

        public double? AsNullableDouble {
            get { return (bsonType == BsonType.Null) ? null : (double?) AsDouble; }
        }

        public Guid? AsNullableGuid {
            get { return (bsonType == BsonType.Null) ? null : (Guid?) AsGuid; }
        }

        public int? AsNullableInt32 {
            get { return (bsonType == BsonType.Null) ? null : (int?) AsInt32; }
        }

        public long? AsNullableInt64 {
            get { return (bsonType == BsonType.Null) ? null : (long?) AsInt64; }
        }

        public ObjectId? AsNullableObjectId {
            get { return (bsonType == BsonType.Null) ? null : (ObjectId?) AsObjectId; }
        }

        public ObjectId AsObjectId {
            get { return ((BsonObjectId) this).Value; }
        }

        public Regex AsRegex {
            get { return ((BsonRegularExpression) this).ToRegex(); }
        }

        public string AsString {
            get { return ((BsonString) this).Value; }
        }

        public BsonType BsonType {
            get { return bsonType; }
        }

        public bool IsBoolean {
            get { return bsonType == BsonType.Boolean; }
        }

        public bool IsBsonArray {
            get { return bsonType == BsonType.Array; }
        }

        public bool IsBsonBinaryData {
            get { return bsonType == BsonType.Binary; }
        }

        public bool IsBsonDocument {
            get { return bsonType == BsonType.Document; }
        }

        public bool IsBsonJavaScript {
            get { return bsonType == BsonType.JavaScript || bsonType == BsonType.JavaScriptWithScope; }
        }

        public bool IsBsonJavaScriptWithScope {
            get { return bsonType == BsonType.JavaScriptWithScope; }
        }

        public bool IsBsonMaxKey {
            get { return bsonType == BsonType.MaxKey; }
        }

        public bool IsBsonMinKey {
            get { return bsonType == BsonType.MinKey; }
        }

        public bool IsBsonNull {
            get { return bsonType == BsonType.Null; }
        }

        public bool IsBsonRegularExpression {
            get { return bsonType == BsonType.RegularExpression; }
        }

        public bool IsBsonSymbol {
            get { return bsonType == BsonType.Symbol; }
        }

        public bool IsBsonTimestamp {
            get { return bsonType == BsonType.Timestamp; }
        }

        public bool IsDateTime {
            get { return bsonType == BsonType.DateTime; }
        }

        public bool IsDouble {
            get { return bsonType == BsonType.Double; }
        }

        public bool IsGuid {
            get { return bsonType == BsonType.Binary && ((BsonBinaryData) this).SubType == BsonBinarySubType.Uuid; }
        }

        public bool IsInt32 {
            get { return bsonType == BsonType.Int32; }
        }

        public bool IsInt64 {
            get { return bsonType == BsonType.Int64; }
        }

        public bool IsNumeric {
            get {
                return
                    bsonType == BsonType.Double ||
                    bsonType == BsonType.Int32 ||
                    bsonType == BsonType.Int64;
            }
        }

        public bool IsObjectId {
            get { return bsonType == BsonType.ObjectId; }
        }

        public bool IsString {
            get { return bsonType == BsonType.String; }
        }

        // note: don't change return value to "this" or lots of things will break
        public virtual object RawValue {
            get { return null; } // subclasses that have a single value (e.g. Int32) override this
        }
        #endregion

        #region public operators
        public static explicit operator bool(
            BsonValue value
        ) {
            return value.AsBoolean;
        }

        public static explicit operator bool?(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsNullableBoolean;
        }

        public static implicit operator BsonValue(
            bool value
        ) {
            return BsonBoolean.Create(value);
        }

        public static implicit operator BsonValue(
            bool? value
        ) {
            return value.HasValue ? (BsonValue) BsonBoolean.Create(value.Value) : BsonNull.Value;
        }

        public static implicit operator BsonValue(
            byte[] value
        ) {
            return BsonBinaryData.Create(value);
        }

        public static implicit operator BsonValue(
            DateTime value
        ) {
            return new BsonDateTime(value);
        }

        public static implicit operator BsonValue(
            DateTime? value
        ) {
            return value.HasValue ? (BsonValue) BsonDateTime.Create(value.Value) : BsonNull.Value;
        }

        public static implicit operator BsonValue(
            double value
        ) {
            return new BsonDouble(value);
        }

        public static implicit operator BsonValue(
            double? value
        ) {
            return value.HasValue ? (BsonValue) BsonDouble.Create(value.Value) : BsonNull.Value;
        }

        public static implicit operator BsonValue(
            Enum value
        ) {
            return BsonTypeMapper.MapToBsonValue(value);
        }

        public static implicit operator BsonValue(
            Guid value
        ) {
            return BsonBinaryData.Create(value);
        }

        public static implicit operator BsonValue(
            Guid? value
        ) {
            return value.HasValue ? (BsonValue) BsonBinaryData.Create(value.Value) : BsonNull.Value;
        }

        public static implicit operator BsonValue(
            int value
        ) {
            return BsonInt32.Create(value);
        }

        public static implicit operator BsonValue(
            int? value
        ) {
            return value.HasValue ? (BsonValue) BsonInt32.Create(value.Value) : BsonNull.Value;
        }

        public static implicit operator BsonValue(
            long value
        ) {
            return new BsonInt64(value);
        }

        public static implicit operator BsonValue(
            long? value
        ) {
            return value.HasValue ? (BsonValue) BsonInt64.Create(value.Value) : BsonNull.Value;
        }

        public static implicit operator BsonValue(
            ObjectId value
        ) {
            return new BsonObjectId(value);
        }

        public static implicit operator BsonValue(
            ObjectId? value
        ) {
            return value.HasValue ? (BsonValue) BsonObjectId.Create(value.Value) : BsonNull.Value;
        }

        public static implicit operator BsonValue(
            Regex value
        ) {
            return BsonRegularExpression.Create(value);
        }

        public static implicit operator BsonValue(
            string value
        ) {
            return BsonString.Create(value);
        }

        public static explicit operator byte[](
            BsonValue value
        ) {
            return (value == null) ? null : value.AsByteArray;
        }

        public static explicit operator DateTime(
            BsonValue value
        ) {
            return value.AsDateTime;
        }

        public static explicit operator DateTime?(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsNullableDateTime;
        }

        public static explicit operator double(
            BsonValue value
        ) {
            return value.AsDouble;
        }

        public static explicit operator double?(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsNullableDouble;
        }

        public static explicit operator Guid(
            BsonValue value
        ) {
            return value.AsGuid;
        }

        public static explicit operator Guid?(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsNullableGuid;
        }

        public static explicit operator int(
            BsonValue value
        ) {
            return value.AsInt32;
        }

        public static explicit operator int?(
            BsonValue value
        ) {
            return value == null ? null : value.AsNullableInt32;
        }

        public static explicit operator long(
            BsonValue value
        ) {
            return value.AsInt64;
        }

        public static explicit operator long?(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsNullableInt64;
        }

        public static explicit operator ObjectId(
            BsonValue value
        ) {
            return value.AsObjectId;
        }

        public static explicit operator ObjectId?(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsNullableObjectId;
        }

        public static explicit operator Regex(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsRegex;
        }

        public static explicit operator string(
            BsonValue value
        ) {
            return (value == null) ? null : value.AsString;
        }

        public static bool operator <(
            BsonValue lhs,
            BsonValue rhs
        ) {
            if (object.ReferenceEquals(lhs, null) && object.ReferenceEquals(rhs, null)) { return false; }
            if (object.ReferenceEquals(lhs, null)) { return true; }
            if (object.ReferenceEquals(rhs, null)) { return false; }
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator <=(
            BsonValue lhs,
            BsonValue rhs
        ) {
            if (object.ReferenceEquals(lhs, null) && object.ReferenceEquals(rhs, null)) { return true; }
            if (object.ReferenceEquals(lhs, null)) { return true; }
            if (object.ReferenceEquals(rhs, null)) { return false; }
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator !=(
            BsonValue lhs,
            BsonValue rhs
        ) {
            return !(lhs == rhs);
        }

        public static bool operator ==(
            BsonValue lhs,
            BsonValue rhs
        ) {
            return object.Equals(lhs, rhs);
        }

        public static bool operator >(
            BsonValue lhs,
            BsonValue rhs
        ) {
            return !(lhs <= rhs);
        }

        public static bool operator >=(
            BsonValue lhs,
            BsonValue rhs
        ) {
            return !(lhs < rhs);
        }
        #endregion

        #region public static methods
        // TODO: implement more Create methods for .NET types (int, string, etc...)? Not sure... already have implicit conversions

        public static BsonValue Create(
            object value
        ) {
            // optimize away the call to MapToBsonValue for the most common cases
            if (value == null) {
                return null;
            } else if (value is BsonValue) {
                return (BsonValue) value;
            } else if (value is int) {
                return BsonInt32.Create((int) value);
            } else if (value is string) {
                return new BsonString((string) value);
            } else if (value is bool) {
                return BsonBoolean.Create((bool) value);
            } else if (value is DateTime) {
                return new BsonDateTime((DateTime) value);
            } else if (value is long) {
                return new BsonInt64((long) value);
            } else if (value is double) {
                return new BsonDouble((double) value);
            } else {
                return BsonTypeMapper.MapToBsonValue(value);
            }
        }

        public static BsonValue ReadFrom(
            BsonReader bsonReader
        ) {
            BsonType bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Array:
                    return BsonArray.ReadFrom(bsonReader);
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    return new BsonBinaryData(bytes, subType);
                case BsonType.Boolean:
                    return BsonBoolean.Create(bsonReader.ReadBoolean());
                case BsonType.DateTime:
                    return new BsonDateTime(bsonReader.ReadDateTime());
                case BsonType.Document:
                    return BsonDocument.ReadFrom(bsonReader);
                case BsonType.Double:
                    return new BsonDouble(bsonReader.ReadDouble());
                case BsonType.Int32:
                    return BsonInt32.Create(bsonReader.ReadInt32());
                case BsonType.Int64:
                    return new BsonInt64(bsonReader.ReadInt64());
                case BsonType.JavaScript:
                    return new BsonJavaScript(bsonReader.ReadJavaScript());
                case BsonType.JavaScriptWithScope:
                    string code = bsonReader.ReadJavaScriptWithScope();
                    var scope = BsonDocument.ReadFrom(bsonReader);
                    return new BsonJavaScriptWithScope(code, scope);
                case BsonType.MaxKey:
                    bsonReader.ReadMaxKey();
                    return BsonMaxKey.Value;
                case BsonType.MinKey:
                    bsonReader.ReadMinKey();
                    return BsonMinKey.Value;
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return BsonNull.Value;
                case BsonType.ObjectId:
                    int timestamp;
                    int machine;
                    short pid;
                    int increment;
                    bsonReader.ReadObjectId(out timestamp, out machine, out pid, out increment);
                    return new BsonObjectId(timestamp, machine, pid, increment);
                case BsonType.RegularExpression:
                    string pattern;
                    string options;
                    bsonReader.ReadRegularExpression(out pattern, out options);
                    return new BsonRegularExpression(pattern, options);
                case BsonType.String:
                    return new BsonString(bsonReader.ReadString());
                case BsonType.Symbol:
                    return BsonSymbol.Create(bsonReader.ReadSymbol());
                case BsonType.Timestamp:
                    return new BsonTimestamp(bsonReader.ReadTimestamp());
                default:
                    var message = string.Format("Invalid BsonType: {0}", bsonType);
                    throw new BsonInternalException(message);
            }
        }
        #endregion

        #region public methods
        public virtual BsonValue Clone() {
            return this; // subclasses override Clone if necessary
        }

        public abstract int CompareTo(
            BsonValue other
        );

        public int CompareTypeTo(
            BsonValue other
        ) {
            if (object.ReferenceEquals(other, null)) { return 1; }
            return bsonTypeSortOrder[bsonType].CompareTo(bsonTypeSortOrder[other.bsonType]);
        }

        public virtual BsonValue DeepClone() {
            return this; // subclasses override DeepClone if necessary
        }

        public bool Equals(
            BsonValue rhs
        ) {
            return object.Equals(this, rhs);
        }

        public override bool Equals(
            object obj
        ) {
            throw new BsonInternalException("A subclass of BsonValue did not override Equals");
        }

        public override int GetHashCode() {
            throw new BsonInternalException("A subclass of BsonValue did not override GetHashCode");
        }

        // ToBoolean follows the JavaScript definition of truthiness
        public bool ToBoolean() {
            switch (bsonType) {
                case BsonType.Boolean: return ((BsonBoolean) this).Value;
                case BsonType.Double: var d = ((BsonDouble) this).Value; return !(double.IsNaN(d) || d == 0.0);
                case BsonType.Int32: return ((BsonInt32) this).Value != 0;
                case BsonType.Int64: return ((BsonInt64) this).Value != 0;
                case BsonType.Null: return false;
                case BsonType.String: return ((BsonString) this).Value != "";
                default: return true; // everything else is true
            }
        }

        public double ToDouble() {
            switch (bsonType) {
                case BsonType.Int32: return (double) ((BsonInt32) this).Value;
                case BsonType.Int64: return (double) ((BsonInt64) this).Value;
                case BsonType.String: return XmlConvert.ToDouble(((BsonString) this).Value);
                default: return ((BsonDouble) this).Value;
            }
        }

        public int ToInt32() {
            switch (bsonType) {
                case BsonType.Double: return (int) ((BsonDouble) this).Value;
                case BsonType.Int64: return (int) ((BsonInt64) this).Value;
                case BsonType.String: return XmlConvert.ToInt32(((BsonString) this).Value);
                default: return ((BsonInt32) this).Value;
            }
        }

        public long ToInt64() {
            switch (bsonType) {
                case BsonType.Double: return (long) ((BsonDouble) this).Value;
                case BsonType.Int32: return (long) ((BsonInt32) this).Value;
                case BsonType.String: return XmlConvert.ToInt64(((BsonString) this).Value);
                default: return ((BsonInt64) this).Value;
            }
        }
        #endregion

        #region internal methods
        internal void WriteTo(
            BsonWriter bsonWriter
        ) {
            switch (bsonType) {
                case BsonType.Array:
                    ((BsonArray) this).WriteTo(bsonWriter);
                    break;
                case BsonType.Binary:
                    var binaryData = (BsonBinaryData) this;
                    bsonWriter.WriteBinaryData(binaryData.Bytes, binaryData.SubType);
                    break;
                case BsonType.Boolean:
                    bsonWriter.WriteBoolean(((BsonBoolean) this).Value);
                    break;
                case BsonType.DateTime:
                    bsonWriter.WriteDateTime(((BsonDateTime) this).Value);
                    break;
                case BsonType.Document:
                    var document = this as BsonDocument;
                    if (document != null) {
                        document.WriteTo(bsonWriter);
                    } else {
                        var documentWrapper = this as BsonDocumentWrapper;
                        if (documentWrapper != null) {
                            documentWrapper.Serialize(bsonWriter, typeof(BsonDocument), null);
                        } else {
                            throw new BsonInternalException("Unexpected class for BsonType document: ", this.GetType().FullName);
                        }
                    }
                    break;
                case BsonType.Double:
                    bsonWriter.WriteDouble(((BsonDouble) this).Value);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(((BsonInt32) this).Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(((BsonInt64) this).Value);
                    break;
                case BsonType.JavaScript:
                    bsonWriter.WriteJavaScript(((BsonJavaScript) this).Code);
                    break;
                case BsonType.JavaScriptWithScope:
                    var script = (BsonJavaScriptWithScope) this;
                    bsonWriter.WriteJavaScriptWithScope(script.Code);
                    script.Scope.WriteTo(bsonWriter);
                    break;
                case BsonType.MaxKey:
                    bsonWriter.WriteMaxKey();
                    break;
                case BsonType.MinKey:
                    bsonWriter.WriteMinKey();
                    break;
                case BsonType.Null:
                    bsonWriter.WriteNull();
                    break;
                case BsonType.ObjectId:
                    var objectId = ((BsonObjectId) this).Value;
                    bsonWriter.WriteObjectId(objectId.Timestamp, objectId.Machine, objectId.Pid, objectId.Increment);
                    break;
                case BsonType.RegularExpression:
                    BsonRegularExpression regex = (BsonRegularExpression) this;
                    bsonWriter.WriteRegularExpression(regex.Pattern, regex.Options);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(((BsonString) this).Value);
                    break;
                case BsonType.Symbol:
                    bsonWriter.WriteSymbol(((BsonSymbol) this).Name);
                    break;
                case BsonType.Timestamp:
                    bsonWriter.WriteTimestamp(((BsonTimestamp) this).Value);
                    break;
            }
        }
        #endregion

        #region explicit IConvertible implementation
        TypeCode IConvertible.GetTypeCode() {
            switch (bsonType) {
                case BsonType.Boolean: return TypeCode.Boolean;
                case BsonType.DateTime: return TypeCode.DateTime;
                case BsonType.Double: return TypeCode.Double;
                case BsonType.Int32: return TypeCode.Int32;
                case BsonType.Int64: return TypeCode.Int64;
                case BsonType.String: return TypeCode.String;
                default: return TypeCode.Object;
            }
        }

        bool IConvertible.ToBoolean(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return this.AsBoolean;
                case BsonType.Double: return Convert.ToBoolean(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToBoolean(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToBoolean(this.AsInt64, provider);
                case BsonType.String: return Convert.ToBoolean(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        byte IConvertible.ToByte(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToByte(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToByte(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToByte(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToByte(this.AsInt64, provider);
                case BsonType.String: return Convert.ToByte(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        char IConvertible.ToChar(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Int32: return Convert.ToChar(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToChar(this.AsInt64, provider);
                case BsonType.String: return Convert.ToChar(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        DateTime IConvertible.ToDateTime(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.DateTime: return this.AsDateTime;
                case BsonType.String: return Convert.ToDateTime(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        decimal IConvertible.ToDecimal(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToDecimal(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToDecimal(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToDecimal(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToDecimal(this.AsInt64, provider);
                case BsonType.String: return Convert.ToDecimal(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        double IConvertible.ToDouble(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToDouble(this.AsBoolean, provider);
                case BsonType.Double: return this.AsDouble;
                case BsonType.Int32: return Convert.ToDouble(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToDouble(this.AsInt64, provider);
                case BsonType.String: return Convert.ToDouble(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        short IConvertible.ToInt16(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToInt16(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToInt16(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToInt16(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToInt16(this.AsInt64, provider);
                case BsonType.String: return Convert.ToInt16(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        int IConvertible.ToInt32(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToInt32(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToInt32(this.AsDouble, provider);
                case BsonType.Int32: return this.AsInt32;
                case BsonType.Int64: return Convert.ToInt32(this.AsInt64, provider);
                case BsonType.String: return Convert.ToInt32(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        long IConvertible.ToInt64(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToInt64(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToInt64(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToInt64(this.AsInt32, provider);
                case BsonType.Int64: return this.AsInt64;
                case BsonType.String: return Convert.ToInt64(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        sbyte IConvertible.ToSByte(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToSByte(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToSByte(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToSByte(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToSByte(this.AsInt64, provider);
                case BsonType.String: return Convert.ToSByte(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        float IConvertible.ToSingle(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToSingle(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToSingle(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToSingle(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToSingle(this.AsInt64, provider);
                case BsonType.String: return Convert.ToSingle(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        string IConvertible.ToString(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToString(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToString(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToString(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToString(this.AsInt64, provider);
                case BsonType.String: return this.AsString;
                default: throw new InvalidCastException();
            }
        }

        object IConvertible.ToType(
            Type conversionType,
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ChangeType(this.AsBoolean, conversionType, provider);
                case BsonType.DateTime: return Convert.ChangeType(this.AsDateTime, conversionType, provider);
                case BsonType.Double: return Convert.ChangeType(this.AsDouble, conversionType, provider);
                case BsonType.Int32: return Convert.ChangeType(this.AsInt32, conversionType, provider);
                case BsonType.Int64: return Convert.ChangeType(this.AsInt64, conversionType, provider);
                case BsonType.String: return Convert.ChangeType(this.AsString, conversionType, provider);
                default: throw new InvalidCastException();
            }
        }

        ushort IConvertible.ToUInt16(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToUInt16(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToUInt16(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToUInt16(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToUInt16(this.AsInt64, provider);
                case BsonType.String: return Convert.ToUInt16(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        uint IConvertible.ToUInt32(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToUInt32(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToUInt32(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToUInt16(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToUInt32(this.AsInt64, provider);
                case BsonType.String: return Convert.ToUInt32(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }

        ulong IConvertible.ToUInt64(
            IFormatProvider provider
        ) {
            switch (bsonType) {
                case BsonType.Boolean: return Convert.ToUInt64(this.AsBoolean, provider);
                case BsonType.Double: return Convert.ToUInt64(this.AsDouble, provider);
                case BsonType.Int32: return Convert.ToUInt64(this.AsInt32, provider);
                case BsonType.Int64: return Convert.ToUInt16(this.AsInt64, provider);
                case BsonType.String: return Convert.ToUInt64(this.AsString, provider);
                default: throw new InvalidCastException();
            }
        }
        #endregion
    }
}
