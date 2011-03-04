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
    /// Represents a BSON JavaScript value.
    /// </summary>
    [Serializable]
    public class BsonJavaScript : BsonValue, IComparable<BsonJavaScript>, IEquatable<BsonJavaScript> {
        #region protected fields
        /// <summary>
        /// The JavaScript code.
        /// </summary>
        protected string code;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonJavaScript class.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public BsonJavaScript(
            string code
        )
            : base(BsonType.JavaScript) {
            this.code = code;
        }

        /// <summary>
        /// Initializes a new instance of the BsonJavaScript class (only called by BsonJavaScriptWithScope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        /// <param name="bsonType">The BsonType (must be JavaScriptWithScope).</param>
        protected BsonJavaScript(
            string code,
            BsonType bsonType
        )
            : base(bsonType) {
            this.code = code;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the JavaScript code.
        /// </summary>
        public string Code {
            get { return code; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a string to a BsonJavaScript.
        /// </summary>
        /// <param name="code">A string.</param>
        /// <returns>A BsonJavaScript.</returns>
        public static implicit operator BsonJavaScript(
            string code
        ) {
            return BsonJavaScript.Create(code);
        }

        /// <summary>
        /// Creates a new instance of the BsonJavaScript class.
        /// </summary>
        /// <param name="code">A string containing JavaScript code.</param>
        /// <returns>A BsonJavaScript.</returns>
        public static BsonJavaScript Create(
            string code
        ) {
            if (code != null) {
                return new BsonJavaScript(code);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new BsonJavaScript.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonJavaScript.</param>
        /// <returns>A BsonJavaScript or null.</returns>
        public new static BsonJavaScript Create(
            object value
        ) {
            if (value != null) {
                return (BsonJavaScript) BsonTypeMapper.MapToBsonValue(value, BsonType.JavaScript);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonJavaScript to another BsonJavaScript.
        /// </summary>
        /// <param name="other">The other BsonJavaScript.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonJavaScript is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonJavaScript other
        ) {
            if (other == null) { return 1; }
            return code.CompareTo(other.code);
        }

        /// <summary>
        /// Compares the BsonJavaScript to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonJavaScript is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherJavaScript = other as BsonJavaScript;
            if (otherJavaScript != null) {
                return CompareTo(otherJavaScript);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonJavaScript to another BsonJavaScript.
        /// </summary>
        /// <param name="rhs">The other BsonJavaScript.</param>
        /// <returns>True if the two BsonJavaScript values are equal.</returns>
        public bool Equals(
            BsonJavaScript rhs
        ) {
            if (rhs == null) { return false; }
            return this.code == rhs.code;
        }

        /// <summary>
        /// Compares this BsonJavaScript to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonJavaScript and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonJavaScript); // works even if obj is null
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + code.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString() {
            return code;
        }
        #endregion
    }
}
