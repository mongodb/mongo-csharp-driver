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
    [Serializable]
    public class BsonRegularExpression : BsonValue, IComparable<BsonRegularExpression>, IEquatable<BsonRegularExpression> {
        #region private fields
        private string pattern;
        private string options;
        #endregion

        #region constructors
        public BsonRegularExpression(
            string pattern
        )
            : base(BsonType.RegularExpression) {
            if (pattern[0] == '/') {
                var index = pattern.LastIndexOf('/');
                this.pattern = pattern.Substring(1, index - 1);
                this.options = pattern.Substring(index + 1);
            } else {
                this.pattern = pattern;
                this.options = "";
            }
        }

        public BsonRegularExpression(
            string pattern,
            string options
        )
            : base(BsonType.RegularExpression) {
            this.pattern = pattern;
            this.options = options ?? "";
        }

        public BsonRegularExpression(
            Regex regex
        )
            : base(BsonType.RegularExpression) {
            this.pattern = regex.ToString();
            // TODO: figure out how other .NET options map to JavaScript options
            this.options = "";
            if ((regex.Options & RegexOptions.IgnoreCase) != 0) {
                this.options += "i";
            }
        }
        #endregion

        #region public properties
        public string Pattern {
            get { return pattern; }
        }

        public string Options {
            get { return options; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonRegularExpression(
            Regex value
        ) {
            return BsonRegularExpression.Create(value);
        }

        public static implicit operator BsonRegularExpression(
            string value
        ) {
            return BsonRegularExpression.Create(value);
        }
        #endregion

        #region public methods
        public new static BsonRegularExpression Create(
            object value
        ) {
            if (value != null) {
                return (BsonRegularExpression) BsonTypeMapper.MapToBsonValue(value, BsonType.RegularExpression);
            } else {
                return null;
            }
        }

        public static BsonRegularExpression Create(
            Regex regex
        ) {
            if (regex != null) {
                return new BsonRegularExpression(regex);
            } else {
                return null;
            }
        }

        public static BsonRegularExpression Create(
            string pattern
        ) {
            if (pattern != null) {
                return new BsonRegularExpression(pattern);
            } else {
                return null;
            }
        }

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
        public int CompareTo(
            BsonRegularExpression other
        ) {
            if (other == null) { return 1; }
            int r = pattern.CompareTo(other.pattern);
            if (r != 0) { return r; }
            return options.CompareTo(other.options);
        }

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

        public bool Equals(
            BsonRegularExpression rhs
        ) {
            if (rhs == null) { return false; }
            return this.pattern == rhs.pattern && this.options == rhs.options;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonRegularExpression); // works even if obj is null
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + pattern.GetHashCode();
            hash = 37 * hash + options.GetHashCode();
            return hash;
        }

        public Regex ToRegex() {
            // TODO: figure out how other JavaScript options map to .NET options
            var options = RegexOptions.None;
            if (this.options.Contains("i")) {
                options |= RegexOptions.IgnoreCase;
            }
            return new Regex(pattern, options);
        }

        public override string ToString() {
            return string.Format("/{0}/{1}", pattern, options);
        }
        #endregion
    }
}
