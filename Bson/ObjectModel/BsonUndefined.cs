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
    /// Represents the BSON undefined value.
    /// </summary>
    [Serializable]
    public class BsonUndefined : BsonValue, IComparable<BsonUndefined>, IEquatable<BsonUndefined> {
        #region private static fields
        private static BsonUndefined value = new BsonUndefined();
        #endregion

        #region constructors
        // private so only the singleton instance can be created
        private BsonUndefined()
            : base(BsonType.Undefined) {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets the singleton instance of BsonUndefined.
        /// </summary>
        public static BsonUndefined Value { get { return value; } }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonUndefined to another BsonUndefined.
        /// </summary>
        /// <param name="other">The other BsonUndefined.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonUndefined is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonUndefined other
        ) {
            if (other == null) { return 1; }
            return 0; // it's a singleton
        }

        /// <summary>
        /// Compares the BsonUndefined to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonUndefined is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            if (other is BsonMinKey) { return 1; }
            if (other is BsonUndefined) { return 0; }
            return -1;
        }

        /// <summary>
        /// Compares this BsonUndefined to another BsonUndefined.
        /// </summary>
        /// <param name="rhs">The other BsonUndefined.</param>
        /// <returns>True if the two BsonUndefined values are equal.</returns>
        public bool Equals(
            BsonUndefined rhs
        ) {
            return rhs != null; // it's a singleton
        }

        /// <summary>
        /// Compares this BsonUndefined to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonUndefined and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonUndefined); // works even if obj is null
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
            return "BsonUndefined";
        }
        #endregion
    }
}
