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
    // TODO: [Serializable] // must have custom deserialization to do SymbolTable lookup
    public class BsonSymbol : BsonValue, IComparable<BsonSymbol>, IEquatable<BsonSymbol> {
        #region private fields
        private string name;
        #endregion

        #region constructors
        // internal because only BsonSymbolTable should call this constructor
        internal BsonSymbol(
            string name
        )
            : base(BsonType.Symbol) {
            this.name = name;
        }
        #endregion

        #region public properties
        public string Name {
            get { return name; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonSymbol(
            string name
        ) {
            return BsonSymbol.Create(name);
        }
        #endregion

        #region public static methods
        public new static BsonSymbol Create(
            object value
        ) {
            if (value != null) {
                return (BsonSymbol) BsonTypeMapper.MapToBsonValue(value, BsonType.Symbol);
            } else {
                return null;
            }
        }

        public static BsonSymbol Create(
            string name
        ) {
            if (name != null) {
                return BsonSymbolTable.Lookup(name);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        // note: a BsonSymbol is guaranteed to be unique because it must be looked up in BsonSymbolTable
        // therefore the implementations of Equals and GetHashCode are considerably more efficient

        public int CompareTo(
            BsonSymbol other
        ) {
            if (other == null) { return 1; }
            return name.CompareTo(other.name);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherSymbol = other as BsonSymbol;
            if (otherSymbol != null) {
                return name.CompareTo(otherSymbol.Name);
            }
            var otherString = other as BsonString;
            if (otherString != null) {
                return name.CompareTo(otherString.Value);
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonSymbol rhs
        ) {
            return object.ReferenceEquals(this, rhs); // symbols are guaranteed to be unique
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonSymbol); // works even if obj is null
        }

        public override int GetHashCode() {
            return name.GetHashCode();
        }

        public override string ToString() {
            return name;
        }
        #endregion
    }
}
