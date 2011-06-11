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
using System.Text.RegularExpressions;

namespace MongoDB.Bson {
    /// <summary>
    /// Represents a BSON regular expression value.
    /// </summary>
    [Serializable]
    public class BsonRegularExpression : BsonValue, IComparable<BsonRegularExpression>, IEquatable<BsonRegularExpression> {
        #region private fields
        private string pattern;
        private string options;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonRegularExpression class.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        public BsonRegularExpression(
            string pattern
        )
            : base(BsonType.RegularExpression) {
            if (pattern.Length > 0 && pattern[0] == '/') {
                var index = pattern.LastIndexOf('/');
                var escaped = pattern.Substring(1, index - 1);
                var unescaped = (escaped == "(?:)") ? "" : Regex.Replace(escaped, @"\\(.)", "$1");
                this.pattern = unescaped;
                this.options = pattern.Substring(index + 1);
            } else {
                this.pattern = pattern;
                this.options = "";
            }
        }

        /// <summary>
        /// Initializes a new instance of the BsonRegularExpression class.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">Regular expression options.</param>
        public BsonRegularExpression(
            string pattern,
            string options
        )
            : base(BsonType.RegularExpression) {
            this.pattern = pattern;
            this.options = options ?? "";
        }

        /// <summary>
        /// Initializes a new instance of the BsonRegularExpression class.
        /// </summary>
        /// <param name="regex">A Regex.</param>
        public BsonRegularExpression(
            Regex regex
        )
            : base(BsonType.RegularExpression) {
            this.pattern = regex.ToString();
            this.options = "";
            if ((regex.Options & RegexOptions.IgnoreCase) != 0) {
                this.options += "i";
            }
            if ((regex.Options & RegexOptions.Multiline) != 0) {
                this.options += "m";
            }
            if ((regex.Options & RegexOptions.IgnorePatternWhitespace) != 0) {
                this.options += "x";
            }
            if ((regex.Options & RegexOptions.Singleline) != 0) {
                this.options += "s";
            }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the regular expression pattern.
        /// </summary>
        public string Pattern {
            get { return pattern; }
        }

        /// <summary>
        /// Gets the regular expression options.
        /// </summary>
        public string Options {
            get { return options; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Converts a Regex to a BsonRegularExpression.
        /// </summary>
        /// <param name="value">A Regex.</param>
        /// <returns>A BsonRegularExpression.</returns>
        public static implicit operator BsonRegularExpression(
            Regex value
        ) {
            return BsonRegularExpression.Create(value);
        }

        /// <summary>
        /// Converts a string to a BsonRegularExpression.
        /// </summary>
        /// <param name="value">A string.</param>
        /// <returns>A BsonRegularExpression.</returns>
        public static implicit operator BsonRegularExpression(
            string value
        ) {
            return BsonRegularExpression.Create(value);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a new BsonRegularExpression.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonRegularExpression.</param>
        /// <returns>A BsonRegularExpression or null.</returns>
        public new static BsonRegularExpression Create(
            object value
        ) {
            if (value != null) {
                return (BsonRegularExpression) BsonTypeMapper.MapToBsonValue(value, BsonType.RegularExpression);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of the BsonRegularExpression class.
        /// </summary>
        /// <param name="regex">A Regex.</param>
        /// <returns>A BsonRegularExpression.</returns>
        public static BsonRegularExpression Create(
            Regex regex
        ) {
            if (regex != null) {
                return new BsonRegularExpression(regex);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of the BsonRegularExpression class.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <returns>A BsonRegularExpression.</returns>
        public static BsonRegularExpression Create(
            string pattern
        ) {
            if (pattern != null) {
                return new BsonRegularExpression(pattern);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of the BsonRegularExpression class.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">Regular expression options.</param>
        /// <returns>A BsonRegularExpression.</returns>
        public static BsonRegularExpression Create(
            string pattern,
            string options
        ) {
            if (pattern != null) {
                return new BsonRegularExpression(pattern, options);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this BsonRegularExpression to another BsonRegularExpression.
        /// </summary>
        /// <param name="other">The other BsonRegularExpression.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonRegularExpression is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonRegularExpression other
        ) {
            if (other == null) { return 1; }
            int r = pattern.CompareTo(other.pattern);
            if (r != 0) { return r; }
            return options.CompareTo(other.options);
        }

        /// <summary>
        /// Compares the BsonRegularExpression to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonRegularExpression is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherRegularExpression = other as BsonRegularExpression;
            if (otherRegularExpression != null) {
                return options.CompareTo(otherRegularExpression);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonRegularExpression to another BsonRegularExpression.
        /// </summary>
        /// <param name="rhs">The other BsonRegularExpression.</param>
        /// <returns>True if the two BsonRegularExpression values are equal.</returns>
        public bool Equals(
            BsonRegularExpression rhs
        ) {
            if (rhs == null) { return false; }
            return this.pattern == rhs.pattern && this.options == rhs.options;
        }

        /// <summary>
        /// Compares this BsonRegularExpression to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonRegularExpression and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonRegularExpression); // works even if obj is null
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + pattern.GetHashCode();
            hash = 37 * hash + options.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts the BsonRegularExpression to a Regex.
        /// </summary>
        /// <returns>A Regex.</returns>
        public Regex ToRegex() {
            var options = RegexOptions.None;
            if (this.options.Contains("i")) {
                options |= RegexOptions.IgnoreCase;
            }
            if (this.options.Contains("m")) {
                options |= RegexOptions.Multiline;
            }
            if (this.options.Contains("x")) {
                options |= RegexOptions.IgnorePatternWhitespace;
            }
            if (this.options.Contains("s")) {
                options |= RegexOptions.Singleline;
            }
            return new Regex(pattern, options);
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString() {
            var escaped = pattern.Replace(@"\", @"\\").Replace("/", @"\/");
            return string.Format("/{0}/{1}", escaped, options);
        }
        #endregion
    }
}
