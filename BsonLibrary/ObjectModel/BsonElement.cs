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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary {
    [Serializable]
    public class BsonElement : IComparable<BsonElement>, IEquatable<BsonElement> {
        #region private fields
        private string name;
        private BsonValue value;
        #endregion

        #region constructors
        // NOTE: for every public BsonElement constructor there is a matching constructor, Add and Set method in BsonDocument

        // used when cloning an existing element, caller will set name and value
        private BsonElement() {
        }

        public BsonElement(
            string name,
            BsonValue value
        ) {
            ValidateElementName(name);
            this.name = name;
            this.value = value;
        }
        #endregion

        #region public properties
        public string Name {
            get { return name; }
        }

        public BsonValue Value {
            get { return value; }
            set { this.value = value; }
        }
        #endregion

        #region public operators
        public static bool operator ==(
            BsonElement lhs,
            BsonElement rhs
        ) {
            return object.Equals(lhs, rhs);
        }

        public static bool operator !=(
            BsonElement lhs,
            BsonElement rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        public static BsonElement Create(
            bool condition,
            string name,
            BsonValue value
        ) {
            if (condition && value != null) {
                return new BsonElement(name, value);
            } else {
                return null;
            }
        }

        public static BsonElement Create(
            string name,
            BsonValue value
        ) {
            if (value != null) {
                return new BsonElement(name, value);
            } else {
                return null;
            }
        }
        #endregion

        #region internal static methods
        internal static BsonElement ReadFrom(
            BsonReader bsonReader
        ) {
            BsonType bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.EndOfDocument) {
                return null;
            }

            string name;
            BsonValue value; // computed in switch statement below

            switch (bsonType) {
                case BsonType.Double:
                    value = new BsonDouble(bsonReader.ReadDouble(out name));
                    break;
                case BsonType.String:
                    value = new BsonString(bsonReader.ReadString(out name));
                    break;
                case BsonType.Document:
                    bsonReader.ReadDocumentName(out name);
                    bsonReader.ReadStartDocument();
                    BsonDocument document = new BsonDocument();
                    BsonElement documentElement;
                    while ((documentElement = BsonElement.ReadFrom(bsonReader)) != null) {
                        document.Add(documentElement);
                    }
                    bsonReader.ReadEndDocument();
                    value = document;
                    break;
                case BsonType.Array:
                    bsonReader.ReadArrayName(out name);
                    bsonReader.ReadStartDocument();
                    BsonArray array = new BsonArray();
                    BsonElement arrayElement;
                    while ((arrayElement = BsonElement.ReadFrom(bsonReader)) != null) {
                        array.Add(arrayElement.Value); // names are ignored on input and regenerated on output
                    }
                    bsonReader.ReadEndDocument();
                    value = array;
                    break;
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out name, out bytes, out subType);
                    value = new BsonBinaryData(bytes, subType);
                    break;
                case BsonType.ObjectId:
                    int timestamp;
                    long machinePidIncrement;
                    bsonReader.ReadObjectId(out name, out timestamp, out machinePidIncrement);
                    value = new BsonObjectId(timestamp, machinePidIncrement);
                    break;
                case BsonType.Boolean:
                    value = BsonBoolean.Create(bsonReader.ReadBoolean(out name));
                    break;
                case BsonType.DateTime:
                    value = new BsonDateTime(bsonReader.ReadDateTime(out name));
                    break;
                case BsonType.Null:
                    bsonReader.ReadNull(out name);
                    value = Bson.Null;
                    break;
                case BsonType.RegularExpression:
                    string pattern;
                    string options;
                    bsonReader.ReadRegularExpression(out name, out pattern, out options);
                    value = new BsonRegularExpression(pattern, options);
                    break;
                case BsonType.JavaScript:
                    value = new BsonJavaScript(bsonReader.ReadJavaScript(out name));
                    break;
                case BsonType.Symbol:
                    value = BsonSymbol.Create(bsonReader.ReadSymbol(out name));
                    break;
                case BsonType.JavaScriptWithScope:
                    string code = bsonReader.ReadJavaScriptWithScope(out name);
                    bsonReader.ReadStartDocument();
                    BsonDocument scope = new BsonDocument();
                    BsonElement scopeElement;
                    while ((scopeElement = BsonElement.ReadFrom(bsonReader)) != null) {
                        scope.Add(scopeElement);
                    }
                    bsonReader.ReadEndDocument();
                    value = new BsonJavaScriptWithScope(code, scope);
                    break;
                case BsonType.Int32:
                    value = BsonInt32.Create(bsonReader.ReadInt32(out name));
                    break;
                case BsonType.Timestamp:
                    value = new BsonTimestamp(bsonReader.ReadTimestamp(out name));
                    break;
                case BsonType.Int64:
                    value = new BsonInt64(bsonReader.ReadInt64(out name));
                    break;
                case BsonType.MinKey:
                    bsonReader.ReadMinKey(out name);
                    value = Bson.MinKey;
                    break;
                case BsonType.MaxKey:
                    bsonReader.ReadMaxKey(out name);
                    value = Bson.MaxKey;
                    break;
                default:
                    throw new BsonInternalException("Unexpected BsonType");
            }

            return new BsonElement(name, value);
        }
        #endregion

        #region private static methods
        private static void ValidateElementName(
            string name
        ) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            if (
                name == "" ||
                name.Contains('\0')
            ) {
                throw new ArgumentException("Invalid element name", "name");
            }
        }
        #endregion

        #region public methods
        public BsonElement Clone() {
            var clone = new BsonElement();
            clone.name = name;
            clone.value = value.Clone();
            return clone;
        }

        public BsonElement DeepClone() {
            var clone = new BsonElement();
            clone.name = name;
            clone.value = value.DeepClone();
            return clone;
        }

        public int CompareTo(
            BsonElement other
        ) {
            if (other == null) { return 1; }
            int r = this.name.CompareTo(other.name);
            if (r != 0) { return r; }
            return this.value.CompareTo(other.value);
        }

        public bool Equals(
            BsonElement rhs
        ) {
            if (rhs == null) { return false; }
            return this.name == rhs.name && this.value == rhs.value;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonElement); // works even if obj is null or of a different type
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + name.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return string.Format("{0}={1}", name, value);
        }
        #endregion

        #region internal methods
        internal void WriteTo(
            BsonWriter bsonWriter
        ) {
            switch (value.BsonType) {
                case BsonType.Double:
                    bsonWriter.WriteDouble(name, value.AsDouble);
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(name, value.AsString);
                    break;
                case BsonType.Document:
                    bsonWriter.WriteDocumentName(name);
                    var document = value as BsonDocument;
                    if (document != null) {
                        document.WriteTo(bsonWriter);
                    } else {
                        var documentWrapper = value as BsonDocumentWrapper;
                        documentWrapper.Serialize(bsonWriter, false); // don't serializeIdFirst
                    }
                    break;
                case BsonType.Array:
                    bsonWriter.WriteArrayName(name);
                    BsonArray array = value.AsBsonArray;
                    array.WriteTo(bsonWriter);
                    break;
                case BsonType.Binary:
                    BsonBinaryData binaryData = value.AsBsonBinaryData;
                    bsonWriter.WriteBinaryData(name, binaryData.Bytes, binaryData.SubType);
                    break;
                case BsonType.ObjectId:
                    var objectId = value.AsObjectId;
                    bsonWriter.WriteObjectId(name, objectId.Timestamp, objectId.MachinePidIncrement);
                    break;
                case BsonType.Boolean:
                    bsonWriter.WriteBoolean(name, value.AsBoolean);
                    break;
                case BsonType.DateTime:
                    bsonWriter.WriteDateTime(name, value.AsDateTime);
                    break;
                case BsonType.Null:
                    bsonWriter.WriteNull(name);
                    break;
                case BsonType.RegularExpression:
                    BsonRegularExpression regex = value.AsBsonRegularExpression;
                    bsonWriter.WriteRegularExpression(name, regex.Pattern, regex.Options);
                    break;
                case BsonType.JavaScript:
                    bsonWriter.WriteJavaScript(name, value.AsBsonJavaScript.Code);
                    break;
                case BsonType.Symbol:
                    bsonWriter.WriteSymbol(name, value.AsBsonSymbol.Name);
                    break;
                case BsonType.JavaScriptWithScope:
                    BsonJavaScriptWithScope javaScriptWithScope = value.AsBsonJavaScriptWithScope;
                    bsonWriter.WriteJavaScriptWithScope(name, javaScriptWithScope.Code);
                    javaScriptWithScope.Scope.WriteTo(bsonWriter);
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(name, value.AsInt32);
                    break;
                case BsonType.Timestamp:
                    bsonWriter.WriteTimestamp(name, value.AsBsonTimestamp.Value);
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(name, value.AsInt64);
                    break;
                case BsonType.MinKey:
                    bsonWriter.WriteMinKey(name);
                    break;
                case BsonType.MaxKey:
                    bsonWriter.WriteMaxKey(name);
                    break;
            }
        }
        #endregion
    }
}
