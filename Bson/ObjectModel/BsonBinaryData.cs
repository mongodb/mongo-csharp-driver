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
    /// Represents BSON binary data.
    /// </summary>
    [Serializable]
    public class BsonBinaryData : BsonValue, IComparable<BsonBinaryData>, IEquatable<BsonBinaryData> {
        #region private fields
        private byte[] bytes;
        private BsonBinarySubType subType;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryData class.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        public BsonBinaryData(
            byte[] bytes
        )
            : base(BsonType.Binary) {
            this.bytes = bytes;
            this.subType = BsonBinarySubType.Binary;
        }

        /// <summary>
        /// Initializes a new instance of the BsonBinaryData class.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public BsonBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        )
            : base(BsonType.Binary) {
            this.bytes = bytes;
            this.subType = subType;
        }

        /// <summary>
        /// Initializes a new instance of the BsonBinaryData class.
        /// </summary>
        /// <param name="guid">A Guid.</param>
        public BsonBinaryData(
            Guid guid
        )
            : base(BsonType.Binary) {
            this.bytes = guid.ToByteArray();
            this.subType = BsonBinarySubType.Uuid;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the binary data.
        /// </summary>
        public byte[] Bytes {
            get { return bytes; }
        }

        /// <summary>
        /// Gets the BsonBinaryData as a Guid if the subtype is Uuid, otherwise null.
        /// </summary>
        public override object RawValue {
            get {
                if (bytes.Length == 16 && subType == BsonBinarySubType.Uuid) {
                    return new Guid(bytes);
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the binary data subtype.
        /// </summary>
        public BsonBinarySubType SubType {
            get { return subType; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a byte array to a BsonBinaryData.
        /// </summary>
        /// <param name="value">A byte array.</param>
        /// <returns>A BsonBinaryData.</returns>
        public static implicit operator BsonBinaryData(
            byte[] value
        ) {
            return BsonBinaryData.Create(value);
        }

        /// <summary>
        /// Converts a Guid to a BsonBinaryData.
        /// </summary>
        /// <param name="value">A Guid.</param>
        /// <returns>A BsonBinaryData.</returns>
        public static implicit operator BsonBinaryData(
            Guid value
        ) {
            return BsonBinaryData.Create(value);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new BsonBinaryData.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <returns>A BsonBinaryData or null.</returns>
        public static BsonBinaryData Create(
            byte[] bytes
        ) {
            return Create(bytes, BsonBinarySubType.Binary);
        }

        /// <summary>
        /// Creates a new BsonBinaryData.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <returns>A BsonBinaryData or null.</returns>
        public static BsonBinaryData Create(
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            if (bytes != null) {
                return new BsonBinaryData(bytes, subType);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new BsonBinaryData.
        /// </summary>
        /// <param name="guid">A Guid.</param>
        /// <returns>A BsonBinaryData.</returns>
        public static BsonBinaryData Create(
            Guid guid
        ) {
            return new BsonBinaryData(guid);
        }

        /// <summary>
        /// Creates a new BsonBinaryData.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonBinaryData.</param>
        /// <returns>A BsonBinaryData or null.</returns>
        public new static BsonBinaryData Create(
            object value
        ) {
            if (value != null) {
                return (BsonBinaryData) BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonBinaryData to another BsonBinaryData.
        /// </summary>
        /// <param name="other">The other BsonBinaryData.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonBinaryData is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonBinaryData other
        ) {
            if (other == null) { return 1; }
            int r = subType.CompareTo(other.subType);
            if (r != 0) { return r; }
            for (int i = 0; i < bytes.Length && i < other.bytes.Length; i++) {
                r = bytes[i].CompareTo(other.bytes[i]);
                if (r != 0) { return r; }
            }
            return bytes.Length.CompareTo(other.bytes.Length);
        }

        /// <summary>
        /// Compares the BsonBinaryData to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonBinaryData is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherBinaryData = other as BsonBinaryData;
            if (otherBinaryData != null) {
                return CompareTo(otherBinaryData);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonBinaryData to another BsonBinaryData.
        /// </summary>
        /// <param name="rhs">The other BsonBinaryData.</param>
        /// <returns>True if the two BsonBinaryData values are equal.</returns>
        public bool Equals(
            BsonBinaryData rhs
        ) {
            if (rhs == null) { return false; }
            return object.ReferenceEquals(this, rhs) || this.subType == rhs.subType && this.bytes.SequenceEqual(rhs.bytes);
        }

        /// <summary>
        /// Compares this BsonBinaryData to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonBinaryData and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonBinaryData); // works even if obj is null
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            foreach (byte b in bytes) {
                hash = 37 * hash + b;
            }
            hash = 37 * hash + subType.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts this BsonBinaryData to a Guid.
        /// </summary>
        /// <returns>A Guid.</returns>
        public Guid ToGuid() {
            if (subType == BsonBinarySubType.Uuid) {
                return new Guid(bytes);
            } else {
                throw new InvalidOperationException("BinaryData subtype is not UUID");
            }
        }

        /// <summary>
        /// Returns a string representation of the binary data.
        /// </summary>
        /// <returns>A string representation of the binary data.</returns>
        public override string ToString() {
            return string.Format("{0}:0x{1}", subType, BsonUtils.ToHexString(bytes));
        }
        #endregion
    }
}
