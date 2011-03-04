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
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MongoDB.Bson {
    /// <summary>
    /// Represents a BSON ObjectId value (see also ObjectId).
    /// </summary>
    [Serializable]
    public class BsonObjectId : BsonValue, IComparable<BsonObjectId>, IEquatable<BsonObjectId> {
        #region private static fields
        private static BsonObjectId emptyInstance = new BsonObjectId(ObjectId.Empty);
        #endregion

        #region private fields
        private ObjectId value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonObjectId(
            ObjectId value
        )
            : base(BsonType.ObjectId) {
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonObjectId(
            byte[] value
        )
            : base(BsonType.ObjectId) {
            this.value = new ObjectId(value);
        }

        /// <summary>
        /// Initializes a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public BsonObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        )
            : base(BsonType.ObjectId) {
            this.value = new ObjectId(timestamp, machine, pid, increment);
        }

        /// <summary>
        /// Initializes a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonObjectId(
            string value
        )
            : base(BsonType.ObjectId) {
            this.value = new ObjectId(value);
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of BsonObjectId where the value is empty.
        /// </summary>
        public static BsonObjectId Empty {
            get { return emptyInstance; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public int Timestamp {
            get { return value.Timestamp; }
        }

        /// <summary>
        /// Gets the machine.
        /// </summary>
        public int Machine {
            get { return value.Machine; }
        }

        /// <summary>
        /// Gets the PID.
        /// </summary>
        public short Pid {
            get { return value.Pid; }
        }

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public int Increment {
            get { return value.Increment; }
        }

        /// <summary>
        /// Gets the creation time (derived from the timestamp).
        /// </summary>
        public DateTime CreationTime {
            get { return value.CreationTime; }
        }

        /// <summary>
        /// Gets the BsonObjectId as an ObjectId.
        /// </summary>
        public override object RawValue {
            get { return value; }
        }

        /// <summary>
        /// Gets the value of this BsonObjectId.
        /// </summary>
        public ObjectId Value {
            get { return value; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts an ObjectId to a BsonObjectId.
        /// </summary>
        /// <param name="value">An ObjectId.</param>
        /// <returns>A BsonObjectId.</returns>
        public static implicit operator BsonObjectId(
            ObjectId value
        ) {
            return new BsonObjectId(value);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="value">An ObjectId.</param>
        /// <returns>A BsonObjectId.</returns>
        public static BsonObjectId Create(
            ObjectId value
        ) {
            return new BsonObjectId(value);
        }

        /// <summary>
        /// Creates a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="value">A byte array.</param>
        /// <returns>A BsonObjectId.</returns>
        public static BsonObjectId Create(
            byte[] value
        ) {
            if (value != null) {
                return new BsonObjectId(value);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The pid.</param>
        /// <param name="increment">The increment.</param>
        /// <returns>A BsonObjectId.</returns>
        public static BsonObjectId Create(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            return new BsonObjectId(timestamp, machine, pid, increment);
        }

        /// <summary>
        /// Creates a new BsonObjectId.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonObjectId.</param>
        /// <returns>A BsonObjectId or null.</returns>
        public new static BsonObjectId Create(
            object value
        ) {
            if (value != null) {
                return (BsonObjectId) BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="value">A string.</param>
        /// <returns>A BsonObjectId.</returns>
        public static BsonObjectId Create(
            string value
        ) {
            if (value != null) {
                return new BsonObjectId(value);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Generates a new BsonObjectId with a unique value.
        /// </summary>
        /// <returns>A BsonObjectId.</returns>
        public static BsonObjectId GenerateNewId() {
            return new BsonObjectId(ObjectId.GenerateNewId());
        }

        /// <summary>
        /// Parses a string and creates a new BsonObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <returns>A BsonObjectId.</returns>
        public static BsonObjectId Parse(
            string s
        ) {
            return new BsonObjectId(ObjectId.Parse(s));
        }

        /// <summary>
        /// Tries to parse a string and create a new BsonObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <param name="value">The new BsonObjectId.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(
            string s,
            out BsonObjectId value
        ) {
            ObjectId objectId;
            if (ObjectId.TryParse(s, out objectId)) {
                value = new BsonObjectId(objectId);
                return true;
            } else {
                value = null;
                return false;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonObjectId to another BsonObjectId.
        /// </summary>
        /// <param name="other">The other BsonObjectId.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonObjectId is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonObjectId other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.Value);
        }

        /// <summary>
        /// Compares the BsonObjectId to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonObjectId is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherObjectId = other as BsonObjectId;
            if (otherObjectId != null) {
                return value.CompareTo(otherObjectId.Value);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonObjectId to another BsonObjectId.
        /// </summary>
        /// <param name="rhs">The other BsonObjectId.</param>
        /// <returns>True if the two BsonObjectId values are equal.</returns>
        public bool Equals(
            BsonObjectId rhs
        ) {
            if (rhs == null) { return false; }
            return this.Value == rhs.Value;
        }

        /// <summary>
        /// Compares this BsonObjectId to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonObjectId and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonObjectId); // works even if obj is null
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts the BsonObjectId to a byte array.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray() {
            return value.ToByteArray();
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString() {
            return value.ToString();
        }
        #endregion
    }
}
