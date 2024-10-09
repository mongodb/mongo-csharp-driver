/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Shared;

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a BSON ObjectId value (see also ObjectId).
    /// </summary>
    public class BsonObjectId : BsonValue, IComparable<BsonObjectId>, IEquatable<BsonObjectId>
    {
        // private static fields
        private static BsonObjectId __emptyInstance = new BsonObjectId(ObjectId.Empty);

        // private fields
        private readonly ObjectId _value;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonObjectId(ObjectId value)
        {
            _value = value;
        }

        // public static properties
        /// <summary>
        /// Gets an instance of BsonObjectId where the value is empty.
        /// </summary>
        public static BsonObjectId Empty
        {
            get { return __emptyInstance; }
        }

        // public properties
        /// <summary>
        /// Gets the BsonType of this BsonValue.
        /// </summary>
        public override BsonType BsonType
        {
            get { return BsonType.ObjectId; }
        }

        /// <summary>
        /// Gets the value of this BsonObjectId.
        /// </summary>
        public ObjectId Value
        {
            get { return _value; }
        }

        // public operators
        /// <summary>
        /// Converts an ObjectId to a BsonObjectId.
        /// </summary>
        /// <param name="value">An ObjectId.</param>
        /// <returns>A BsonObjectId.</returns>
        public static implicit operator BsonObjectId(ObjectId value)
        {
            return new BsonObjectId(value);
        }

        /// <summary>
        /// Compares two BsonObjectId values.
        /// </summary>
        /// <param name="lhs">The first BsonObjectId.</param>
        /// <param name="rhs">The other BsonObjectId.</param>
        /// <returns>True if the two BsonObjectId values are not equal according to ==.</returns>
        public static bool operator !=(BsonObjectId lhs, BsonObjectId rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two BsonObjectId values.
        /// </summary>
        /// <param name="lhs">The first BsonObjectId.</param>
        /// <param name="rhs">The other BsonObjectId.</param>
        /// <returns>True if the two BsonObjectId values are equal according to ==.</returns>
        public static bool operator ==(BsonObjectId lhs, BsonObjectId rhs)
        {
            if (object.ReferenceEquals(lhs, null)) { return object.ReferenceEquals(rhs, null); }
            return lhs.Equals(rhs);
        }

        // public static methods
        /// <summary>
        /// Creates a new BsonObjectId.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonObjectId.</param>
        /// <returns>A BsonObjectId or null.</returns>
        public new static BsonObjectId Create(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return (BsonObjectId)BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
        }

        // public methods
        /// <summary>
        /// Compares this BsonObjectId to another BsonObjectId.
        /// </summary>
        /// <param name="other">The other BsonObjectId.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonObjectId is less than, equal to, or greather than the other.</returns>
        public int CompareTo(BsonObjectId other)
        {
            if (other == null) { return 1; }
            return _value.CompareTo(other.Value);
        }

        /// <summary>
        /// Compares the BsonObjectId to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonObjectId is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(BsonValue other)
        {
            if (other == null) { return 1; }
            var otherObjectId = other as BsonObjectId;
            if (otherObjectId != null)
            {
                return _value.CompareTo(otherObjectId.Value);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonObjectId to another BsonObjectId.
        /// </summary>
        /// <param name="rhs">The other BsonObjectId.</param>
        /// <returns>True if the two BsonObjectId values are equal.</returns>
        public bool Equals(BsonObjectId rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return this.Value == rhs.Value;
        }

        /// <summary>
        /// Compares this BsonObjectId to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonObjectId and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BsonObjectId); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = 37 * hash + Hasher.GetHashCode(BsonType);
            hash = 37 * hash + _value.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            return _value.ToString();
        }

        // protected methods
        /// <inheritdoc/>
        protected override string IConvertibleToStringImplementation(IFormatProvider provider)
        {
            return _value.ToString();
        }
    }
}
