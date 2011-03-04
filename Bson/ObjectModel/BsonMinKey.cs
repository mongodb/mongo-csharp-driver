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
    /// Represents the BSON MinKey value.
    /// </summary>
    [Serializable]
    public class BsonMinKey : BsonValue, IComparable<BsonMinKey>, IEquatable<BsonMinKey> {
        #region private static fields
        private static BsonMinKey value = new BsonMinKey();
        #endregion

        #region constructors
        // private so only the singleton instance can be created
        private BsonMinKey()
            : base(BsonType.MinKey) {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets the singleton instance of BsonMinKey.
        /// </summary>
        public static BsonMinKey Value { get { return value; } }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonMinKey to another BsonMinKey.
        /// </summary>
        /// <param name="other">The other BsonMinKey.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonMinKey is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonMinKey other
        ) {
            if (other == null) { return 1; }
            return 0; // it's a singleton
        }

        /// <summary>
        /// Compares the BsonMinKey to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonMinKey is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            if (other is BsonMinKey) { return 0; }
            return -1;
        }

        /// <summary>
        /// Compares this BsonMinKey to another BsonMinKey.
        /// </summary>
        /// <param name="rhs">The other BsonMinKey.</param>
        /// <returns>True if the two BsonMinKey values are equal.</returns>
        public bool Equals(
            BsonMinKey rhs
        ) {
            return rhs != null; // it's a singleton
        }

        /// <summary>
        /// Compares this BsonMinKey to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonMinKey and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonMinKey); // works even if obj is null
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            return bsonType.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString() {
            return "BsonMinKey";
        }
        #endregion
    }
}
