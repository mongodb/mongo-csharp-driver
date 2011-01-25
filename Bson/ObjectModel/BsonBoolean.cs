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
using System.Xml;

namespace MongoDB.Bson {
    [Serializable]
    public class BsonBoolean : BsonValue, IComparable<BsonBoolean>, IEquatable<BsonBoolean> {
        #region private static fields
        private static BsonBoolean falseInstance = new BsonBoolean(false);
        private static BsonBoolean trueInstance = new BsonBoolean(true);
        #endregion

        #region private fields
        private bool value;
        #endregion

        #region constructors
        // private so that only the two official instances can be created
        private BsonBoolean(
            bool value
        )
            : base(BsonType.Boolean) {
            this.value = value;
        }
        #endregion

        #region public static properties
        public static BsonBoolean False {
            get { return falseInstance; }
        }

        public static BsonBoolean True {
            get { return trueInstance; }
        }
        #endregion

        #region public properties
        public override object RawValue {
            get { return value; }
        }

        public bool Value {
            get { return value; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonBoolean(
            bool value
        ) {
            return BsonBoolean.Create(value);
        }
        #endregion

        #region public static methods
        public static BsonBoolean Create(
            bool value
        ) {
            return value ? trueInstance : falseInstance;
        }

        public new static BsonBoolean Create(
            object value
        ) {
            if (value != null) {
                return (BsonBoolean) BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public int CompareTo(
            BsonBoolean other
        ) {
            if (other == null) { return 1; }
            return (value ? 1 : 0).CompareTo(other.value ? 1 : 0);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherBoolean = other as BsonBoolean;
            if (otherBoolean != null) {
                return (value ? 1 : 0).CompareTo(otherBoolean.value ? 1 : 0);
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonBoolean rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonBoolean); // works even if obj is null
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return XmlConvert.ToString(value);
        }
        #endregion
    }
}
