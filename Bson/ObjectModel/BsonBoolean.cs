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
    /// Represents a BSON boolean value.
    /// </summary>
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
        /// <summary>
        /// Gets the instance of BsonBoolean that represents false.
        /// </summary>
        public static BsonBoolean False {
            get { return falseInstance; }
        }

        /// <summary>
        /// Gets the instance of BsonBoolean that represents true.
        /// </summary>
        public static BsonBoolean True {
            get { return trueInstance; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the BsonBoolean as a bool.
        /// </summary>
        public override object RawValue {
            get { return value; }
        }

        /// <summary>
        /// Gets the value of this BsonBoolean.
        /// </summary>
        public bool Value {
            get { return value; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a bool to a BsonBoolean.
        /// </summary>
        /// <param name="value">A bool.</param>
        /// <returns>A BsonBoolean.</returns>
        public static implicit operator BsonBoolean(
            bool value
        ) {
            return BsonBoolean.Create(value);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Returns one of the two possible BsonBoolean values.
        /// </summary>
        /// <param name="value">The bool value.</param>
        /// <returns>The corresponding BsonBoolean value.</returns>
        public static BsonBoolean Create(
            bool value
        ) {
            return value ? trueInstance : falseInstance;
        }

        /// <summary>
        /// Returns one of the two possible BsonBoolean values.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonBoolean.</param>
        /// <returns>A BsonBoolean or null.</returns>
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
        /// <summary>
        /// Compares this BsonBoolean to another BsonBoolean.
        /// </summary>
        /// <param name="other">The other BsonBoolean.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonBoolean is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonBoolean other
        ) {
            if (other == null) { return 1; }
            return (value ? 1 : 0).CompareTo(other.value ? 1 : 0);
        }

        /// <summary>
        /// Compares the BsonBoolean to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonBoolean is less than, equal to, or greather than the other BsonValue.</returns>
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

        /// <summary>
        /// Compares this BsonBoolean to another BsonBoolean.
        /// </summary>
        /// <param name="rhs">The other BsonBoolean.</param>
        /// <returns>True if the two BsonBoolean values are equal.</returns>
        public bool Equals(
            BsonBoolean rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        /// <summary>
        /// Compares this BsonBoolean to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonBoolean and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonBoolean); // works even if obj is null
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
