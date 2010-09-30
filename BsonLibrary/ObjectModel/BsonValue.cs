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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MongoDB.BsonLibrary {
    public abstract class BsonValue : IComparable<BsonValue>, IEquatable<BsonValue> {
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

        public bool IsObjectId {
            get { return bsonType == BsonType.ObjectId; }
        }

        public bool IsString {
            get { return bsonType == BsonType.String; }
        }

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

        public static implicit operator BsonValue(
            bool value
        ) {
            return BsonBoolean.Create(value);
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
            double value
        ) {
            return new BsonDouble(value);
        }

        public static implicit operator BsonValue(
            Guid value
        ) {
            return BsonBinaryData.Create(value);
        }

        public static implicit operator BsonValue(
            int value
        ) {
            return BsonInt32.Create(value);
        }

        public static implicit operator BsonValue(
            long value
        ) {
            return new BsonInt64(value);
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
            return value.AsByteArray;
        }

        public static explicit operator DateTime(
            BsonValue value
        ) {
            return value.AsDateTime;
        }

        public static explicit operator double(
            BsonValue value
        ) {
            return value.AsDouble;
        }

        public static explicit operator Guid(
            BsonValue value
        ) {
            return value.AsGuid;
        }

        public static explicit operator int(
            BsonValue value
        ) {
            return value.AsInt32;
        }

        public static explicit operator long(
            BsonValue value
        ) {
            return value.AsInt64;
        }

        public static explicit operator Regex(
            BsonValue value
        ) {
            return value.AsRegex;
        }

        public static explicit operator string(
            BsonValue value
        ) {
            return value.AsString;
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
        public virtual bool ToBoolean() {
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
    }
}
