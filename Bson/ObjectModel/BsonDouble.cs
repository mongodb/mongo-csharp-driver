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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace MongoDB.Bson {
    /// <summary>
    /// Represents a BSON double value.
    /// </summary>
    [Serializable]
    public class BsonDouble : BsonValue, IComparable<BsonDouble>, IEquatable<BsonDouble> {
        #region private fields
        private double value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDouble class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonDouble(
            double value
        )
            : base(BsonType.Double) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the BsonDouble as a double.
        /// </summary>
        public override object RawValue {
            get { return value; }
        }

        /// <summary>
        /// Gets the value of this BsonDouble.
        /// </summary>
        public double Value {
            get { return value; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a double to a BsonDouble.
        /// </summary>
        /// <param name="value">A double.</param>
        /// <returns>A BsonDouble.</returns>
        public static implicit operator BsonDouble(
            double value
        ) {
            return new BsonDouble(value);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new instance of the BsonDouble class.
        /// </summary>
        /// <param name="value">A double.</param>
        /// <returns>A BsonDouble.</returns>
        public static BsonDouble Create(
            double value
        ) {
            return new BsonDouble(value);
        }

        /// <summary>
        /// Creates a new instance of the BsonDouble class.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonDouble.</param>
        /// <returns>A BsonDouble.</returns>
        public new static BsonDouble Create(
            object value
        ) {
            if (value != null) {
                return (BsonDouble) BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonDouble to another BsonDouble.
        /// </summary>
        /// <param name="other">The other BsonDouble.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonDouble is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonDouble other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.value);
        }

        /// <summary>
        /// Compares the BsonDouble to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonDouble is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherDouble = other as BsonDouble;
            if (otherDouble != null) {
                return value.CompareTo(otherDouble.value);
            }
            var otherInt32 = other as BsonInt32;
            if (otherInt32 != null) {
                return value.CompareTo((double) otherInt32.Value);
            }
            var otherInt64 = other as BsonInt64;
            if (otherInt64 != null) {
                return value.CompareTo((double) otherInt64.Value);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonDouble to another BsonDouble.
        /// </summary>
        /// <param name="rhs">The other BsonDouble.</param>
        /// <returns>True if the two BsonDouble values are equal.</returns>
        public bool Equals(
            BsonDouble rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        /// <summary>
        /// Compares this BsonDouble to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonDouble and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            if (obj == null) { return false; }
            var rhsDouble = obj as BsonDouble;
            if (rhsDouble != null) {
                return value == rhsDouble.value;
            }
            var rhsInt32 = obj as BsonInt32;
            if (rhsInt32 != null) {
                return value == (double) rhsInt32.Value;
            }
            var rhsInt64 = obj as BsonInt64;
            if (rhsInt64 != null) {
                return value == (double) rhsInt64.Value;
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
            return value.ToString("R", NumberFormatInfo.InvariantInfo);
        }
        #endregion
    }
}
