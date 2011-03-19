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
    /// <summary>
    /// Represents a BSON string value.
    /// </summary>
    [Serializable]
    public class BsonString : BsonValue, IComparable<BsonString>, IEquatable<BsonString> {
        #region private static fields
        private static BsonString emptyInstance = new BsonString("");
        #endregion

        #region private fields
        private string value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonString class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonString(
            string value
        ) 
            : base(BsonType.String) {
            this.value = value;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of BsonString that represents an empty string.
        /// </summary>
        public static BsonString Empty {
            get { return emptyInstance; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the BsonString as a string.
        /// </summary>
        public override object RawValue {
            get { return value; }
        }

        /// <summary>
        /// Gets the value of this BsonString.
        /// </summary>
        public string Value {
            get { return value; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a string to a BsonString.
        /// </summary>
        /// <param name="value">A string.</param>
        /// <returns>A BsonString.</returns>
        public static implicit operator BsonString(
            string value
        ) {
            return BsonString.Create(value);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new BsonString.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonString.</param>
        /// <returns>A BsonString or null.</returns>
        public new static BsonString Create(
            object value
        ) {
            if (value != null) {
                return (BsonString) BsonTypeMapper.MapToBsonValue(value, BsonType.String);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of the BsonString class.
        /// </summary>
        /// <param name="value">A string.</param>
        /// <returns>A BsonString.</returns>
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
        /// <summary>
        /// Compares this BsonString to another BsonString.
        /// </summary>
        /// <param name="other">The other BsonString.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonString is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonString other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.Value);
        }

        /// <summary>
        /// Compares the BsonString to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonString is less than, equal to, or greather than the other BsonValue.</returns>
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

        /// <summary>
        /// Compares this BsonString to another BsonString.
        /// </summary>
        /// <param name="rhs">The other BsonString.</param>
        /// <returns>True if the two BsonString values are equal.</returns>
        public bool Equals(
            BsonString rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        /// <summary>
        /// Compares this BsonString to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonString and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonString); // works even if obj is null
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
            return value;
        }
        #endregion
    }
}
