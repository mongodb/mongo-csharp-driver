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

namespace MongoDB.Bson {
    [Serializable]
    public class BsonString : BsonValue, IComparable<BsonString>, IEquatable<BsonString> {
        #region private static fields
        private static BsonString emptyInstance = new BsonString("");
        #endregion

        #region private fields
        private string value;
        #endregion

        #region constructors
        public BsonString(
            string value
        ) 
            : base(BsonType.String) {
            this.value = value;
        }
        #endregion

        #region public static properties
        public static BsonString Empty {
            get { return emptyInstance; }
        }
        #endregion

        #region public properties
        public override object RawValue {
            get { return value; }
        }

        public string Value {
            get { return value; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonString(
            string value
        ) {
            return BsonString.Create(value);
        }
        #endregion

        #region public static methods
        public new static BsonString Create(
            object value
        ) {
            if (value != null) {
                return (BsonString) BsonTypeMapper.MapToBsonValue(value, BsonType.String);
            } else {
                return null;
            }
        }

        public static BsonString Create(
            string value
        ) {
            if (value != null) {
                // TODO: are there any other commonly used strings worth checking for?
                return value == "" ? emptyInstance : new BsonString(value);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public int CompareTo(
            BsonString other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.Value);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherString = other as BsonString;
            if (otherString != null) {
                return value.CompareTo(otherString.Value);
            }
            var otherSymbol = other as BsonSymbol;
            if (otherSymbol != null) {
                return value.CompareTo(otherSymbol.Name);
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonString rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonString); // works even if obj is null
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return value;
        }
        #endregion
    }
}
