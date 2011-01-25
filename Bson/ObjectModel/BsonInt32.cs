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
    public class BsonInt32 : BsonValue, IComparable<BsonInt32>, IEquatable<BsonInt32> {
        #region private static fields
        private static int firstInstance = -10;
        private static int lastInstance = 100;
        private static BsonInt32[] instances;
        #endregion

        #region private fields
        private int value;
        #endregion

        #region static constructor
        static BsonInt32() {
            instances = new BsonInt32[lastInstance - firstInstance + 1];
            for (int i = 0; i < instances.Length; i++) {
                instances[i] = new BsonInt32(firstInstance + i);
            }
        }
        #endregion

        #region constructors
        public BsonInt32(
            int value
        )
            : base(BsonType.Int32) {
            this.value = value;
        }
        #endregion

        #region public static properties
        public static BsonInt32 MinusOne {
            get { return BsonInt32.Create(-1); }
        }

        public static BsonInt32 Zero {
            get { return BsonInt32.Create(0); }
        }

        public static BsonInt32 One {
            get { return BsonInt32.Create(1); }
        }

        public static BsonInt32 Two {
            get { return BsonInt32.Create(2); }
        }

        public static BsonInt32 Three {
            get { return BsonInt32.Create(3); }
        }
        #endregion

        #region public properties
        public override object RawValue {
            get { return value; }
        }

        public int Value {
            get { return value; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonInt32(
            int value
        ) {
            return BsonInt32.Create(value);
        }
        #endregion

        #region public static methods
        public static BsonInt32 Create(
            int value
        ) {
            if (value >= firstInstance && value <= lastInstance) {
                return instances[value - firstInstance];
            } else {
                return new BsonInt32(value);
            }
        }

        public new static BsonInt32 Create(
            object value
        ) {
            if (value != null) {
                return (BsonInt32) BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public int CompareTo(
            BsonInt32 other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.value);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherInt32 = other as BsonInt32;
            if (otherInt32 != null) {
                return value.CompareTo(otherInt32.value);
            }
            var otherInt64 = other as BsonInt64;
            if (otherInt64 != null) {
                return ((long) value).CompareTo(otherInt64.Value);
            }
            var otherDouble = other as BsonDouble;
            if (otherDouble != null) {
                return ((double) value).CompareTo(otherDouble.Value);
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonInt32 rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        public override bool Equals(
            object obj
        ) {
            if (obj == null) { return false; }
            var rhsInt32 = obj as BsonInt32;
            if (rhsInt32 != null) {
                return this.value == rhsInt32.value;
            }
            var rhsInt64 = obj as BsonInt64;
            if (rhsInt64 != null) {
                return (long) this.value == rhsInt64.Value;
            }
            var rhsDouble = obj as BsonDouble;
            if (rhsDouble != null) {
                return (double) this.value == rhsDouble.Value;
            }
            return false;
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
