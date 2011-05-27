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
    /// Represents a BSON timestamp value.
    /// </summary>
    [Serializable]
    public class BsonTimestamp : BsonValue, IComparable<BsonTimestamp>, IEquatable<BsonTimestamp> {
        #region private fields
        private long value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonTimestamp class.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
        public BsonTimestamp(
            long value
        )
            : base(BsonType.Timestamp) {
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance of the BsonTimestamp class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="increment">The increment.</param>
        public BsonTimestamp(
            int timestamp,
            int increment
        )
            : base(BsonType.Timestamp) {
            this.value = ((long) timestamp << 32) + increment;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the value of this BsonTimestamp.
        /// </summary>
        public long Value {
            get { return value; }
        }

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public int Increment {
            get { return (int) value; }
        }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public int Timestamp {
            get { return (int) (value >> 32); }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new instance of the BsonTimestamp class.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
        /// <returns>A BsonTimestamp.</returns>
        public static BsonTimestamp Create(
            long value
        ) {
            return new BsonTimestamp(value);
        }

        /// <summary>
        /// Creates a new instance of the BsonTimestamp class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="increment">The increment.</param>
        /// <returns>A BsonTimestamp.</returns>
        public static BsonTimestamp Create(
            int timestamp,
            int increment
        ) {
            return new BsonTimestamp(timestamp, increment);
        }

        /// <summary>
        /// Creates a new BsonTimestamp.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonTimestamp.</param>
        /// <returns>A BsonTimestamp or null.</returns>
        public new static BsonTimestamp Create(
            object value
        ) {
            if (value != null) {
                return (BsonTimestamp) BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonTimestamp to another BsonTimestamp.
        /// </summary>
        /// <param name="other">The other BsonTimestamp.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonTimestamp is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonTimestamp other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.value);
        }

        /// <summary>
        /// Compares the BsonTimestamp to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonTimestamp is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherTimestamp = other as BsonTimestamp;
            if (otherTimestamp != null) {
                return value.CompareTo(otherTimestamp.value);
            }
            var otherDateTime = other as BsonDateTime;
            if (otherDateTime != null) {
                var seconds = (int) (otherDateTime.MillisecondsSinceEpoch / 1000);
                var otherTimestampValue = ((long) seconds) << 32;
                return value.CompareTo(otherTimestampValue);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonTimestamp to another BsonTimestamp.
        /// </summary>
        /// <param name="rhs">The other BsonTimestamp.</param>
        /// <returns>True if the two BsonTimestamp values are equal.</returns>
        public bool Equals(
            BsonTimestamp rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        /// <summary>
        /// Compares this BsonTimestamp to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonTimestamp and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonTimestamp); // works even if obj is null
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
