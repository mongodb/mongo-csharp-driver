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
    /// <summary>
    /// Represents a BSON long value.
    /// </summary>
    [Serializable]
    public class BsonInt64 : BsonValue, IComparable<BsonInt64>, IEquatable<BsonInt64> {
        #region private fields
        private long value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonInt64 class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonInt64(
            long value
        )
            : base(BsonType.Int64) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the BsonInt64 as a long.
        /// </summary>
        public override object RawValue {
            get { return value; }
        }

        /// <summary>
        /// Gets the value of this BsonInt64.
        /// </summary>
        public long Value {
            get { return value; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a long to a BsonInt64.
        /// </summary>
        /// <param name="value">A long.</param>
        /// <returns>A BsonInt64.</returns>
        public static implicit operator BsonInt64(
            long value
        ) {
            return new BsonInt64(value);
        }
        #endregion

        #region public statie methods
        /// <summary>
        /// Creates a new instance of the BsonInt64 class.
        /// </summary>
        /// <param name="value">A long.</param>
        /// <returns>A BsonInt64.</returns>
        public static BsonInt64 Create(
            long value
        ) {
            return new BsonInt64(value);
        }

        /// <summary>
        /// Creates a new BsonInt64.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonInt64.</param>
        /// <returns>A BsonInt64 or null.</returns>
        public new static BsonInt64 Create(
            object value
        ) {
            if (value != null) {
                return (BsonInt64) BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonInt64 to another BsonInt64.
        /// </summary>
        /// <param name="other">The other BsonInt64.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonInt64 is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonInt64 other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.value);
        }

        /// <summary>
        /// Compares the BsonInt64 to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonInt64 is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherInt64 = other as BsonInt64;
            if (otherInt64 != null) {
                return value.CompareTo(otherInt64.value);
            }
            var otherInt32 = other as BsonInt32;
            if (otherInt32 != null) {
                return value.CompareTo((long) otherInt32.Value);
            }
            var otherDouble = other as BsonDouble;
            if (otherDouble != null) {
                return ((double) value).CompareTo(otherDouble.Value);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonInt64 to another BsonInt64.
        /// </summary>
        /// <param name="rhs">The other BsonInt64.</param>
        /// <returns>True if the two BsonInt64 values are equal.</returns>
        public bool Equals(
            BsonInt64 rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        /// <summary>
        /// Compares this BsonInt64 to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonInt64 and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            if (obj == null) { return false; }
            var rhsInt64 = obj as BsonInt64;
            if (rhsInt64 != null) {
                return this.value == rhsInt64.value;
            }
            var rhsInt32 = obj as BsonInt32;
            if (rhsInt32 != null) {
                return this.value == (long) rhsInt32.Value;
            }
            var rhsDouble = obj as BsonDouble;
            if (rhsDouble != null) {
                return (double) this.value == rhsDouble.Value;
            }
            return false;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString() {
            return XmlConvert.ToString(value);
        }
        #endregion
    }
}
