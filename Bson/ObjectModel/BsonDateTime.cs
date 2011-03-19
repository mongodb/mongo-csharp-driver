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
    /// Represents a BSON DateTime value.
    /// </summary>
    [Serializable]
    public class BsonDateTime : BsonValue, IComparable<BsonDateTime>, IEquatable<BsonDateTime> {
        #region private fields
        private DateTime value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDateTime class.
        /// </summary>
        /// <param name="value">A DateTime.</param>
        public BsonDateTime(
            DateTime value
        )
            : base(BsonType.DateTime) {
            this.value = value.ToUniversalTime();
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the BsonDateTime as a DateTime.
        /// </summary>
        public override object RawValue {
            get { return value; }
        }

        /// <summary>
        /// Gets the value of this BsonDateTime.
        /// </summary>
        public DateTime Value {
            get { return value; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a DateTime to a BsonDateTime.
        /// </summary>
        /// <param name="value">A DateTime.</param>
        /// <returns>A BsonDateTime.</returns>
        public static implicit operator BsonDateTime(
            DateTime value
        ) {
            return new BsonDateTime(value);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new BsonDateTime.
        /// </summary>
        /// <param name="value">A DateTime.</param>
        /// <returns>A BsonDateTime.</returns>
        public static BsonDateTime Create(
            DateTime value
        ) {
            return new BsonDateTime(value);
        }

        /// <summary>
        /// Creates a new BsonDateTime.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonDateTime.</param>
        /// <returns>A BsonDateTime or null.</returns>
        public new static BsonDateTime Create(
            object value
        ) {
            if (value != null) {
                return (BsonDateTime) BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonDateTime to another BsonDateTime.
        /// </summary>
        /// <param name="other">The other BsonDateTime.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonDateTime is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonDateTime other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.value);
        }

        /// <summary>
        /// Compares the BsonDateTime to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonDateTime is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherDateTime = other as BsonDateTime;
            if (otherDateTime != null) {
                return value.CompareTo(otherDateTime.value);
            }
            var otherTimestamp = other as BsonTimestamp;
            if (otherTimestamp != null) {
                return value.CompareTo(BsonConstants.UnixEpoch.AddSeconds(otherTimestamp.Timestamp));
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonDateTime to another BsonDateTime.
        /// </summary>
        /// <param name="rhs">The other BsonDateTime.</param>
        /// <returns>True if the two BsonDateTime values are equal.</returns>
        public bool Equals(
            BsonDateTime rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        /// <summary>
        /// Compares this BsonDateTime to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonDateTime and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonDateTime); // works even if obj is null
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
            return XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind);
        }
        #endregion
    }
}
