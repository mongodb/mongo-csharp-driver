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
    [Serializable]
    public class BsonJavaScript : BsonValue, IComparable<BsonJavaScript>, IEquatable<BsonJavaScript> {
        #region protected fields
        protected string code;
        #endregion

        #region constructors
        public BsonJavaScript(
            string code
        )
            : base(BsonType.JavaScript) {
            this.code = code;
        }

        // called by BsonJavaScriptWithScope
        protected BsonJavaScript(
            string code,
            BsonType bsonType
        )
            : base(bsonType) {
            this.code = code;
        }
        #endregion

        #region public properties
        public string Code {
            get { return code; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonJavaScript(
            string code
        ) {
            return BsonJavaScript.Create(code);
        }

        public static BsonJavaScript Create(
            string code
        ) {
            if (code != null) {
                return new BsonJavaScript(code);
            } else {
                return null;
            }
        }

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
        public int CompareTo(
            BsonJavaScript other
        ) {
            if (other == null) { return 1; }
            return code.CompareTo(other.code);
        }

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

        public bool Equals(
            BsonJavaScript rhs
        ) {
            if (rhs == null) { return false; }
            return this.code == rhs.code;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonJavaScript); // works even if obj is null
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + code.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return code;
        }
        #endregion
    }
}
