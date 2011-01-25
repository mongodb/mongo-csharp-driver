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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;

namespace MongoDB.Bson {
    [Serializable]
    public class BsonElement : IComparable<BsonElement>, IEquatable<BsonElement> {
        #region private fields
        private string name;
        private BsonValue value;
        #endregion

        #region constructors
        // NOTE: for every public BsonElement constructor there is a matching constructor, Add and Set method in BsonDocument

        // used when cloning an existing element, caller will set name and value
        private BsonElement() {
        }

        public BsonElement(
            string name,
            BsonValue value
        ) {
            ValidateElementName(name);
            this.name = name;
            this.value = value;
        }
        #endregion

        #region public properties
        public string Name {
            get { return name; }
        }

        public BsonValue Value {
            get { return value; }
            set {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
                this.value = value;
            }
        }
        #endregion

        #region public operators
        public static bool operator ==(
            BsonElement lhs,
            BsonElement rhs
        ) {
            return object.Equals(lhs, rhs);
        }

        public static bool operator !=(
            BsonElement lhs,
            BsonElement rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        public static BsonElement Create(
            bool condition,
            string name,
            BsonValue value
        ) {
            if (condition && value != null) {
                return new BsonElement(name, value);
            } else {
                return null;
            }
        }

        public static BsonElement Create(
            string name,
            BsonValue value
        ) {
            if (value != null) {
                return new BsonElement(name, value);
            } else {
                return null;
            }
        }
        #endregion

        #region internal static methods
        internal static bool ReadFrom(
            BsonReader bsonReader,
            out BsonElement element
        ) {
            BsonType bsonType;
            if ((bsonType = bsonReader.ReadBsonType()) != BsonType.EndOfDocument) {
                var name = bsonReader.ReadName();
                var value = BsonValue.ReadFrom(bsonReader);
                element = new BsonElement(name, value);
                return true;
            } else {
                element = null;
                return false;
            }
        }

        internal static BsonElement ReadFrom(
            BsonReader bsonReader,
            string expectedName
        ) {
            BsonElement element;
            if (ReadFrom(bsonReader, out element)) {
                if (element.Name != expectedName) {
                    string message = string.Format("Element name is not {0}", expectedName);
                    throw new FileFormatException(message);
                }
                return element;
            } else {
                string message = string.Format("Element is missing: {0}", expectedName);
                throw new FileFormatException(message);
            }
        }
        #endregion

        #region private static methods
        private static void ValidateElementName(
            string name
        ) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            if (name.IndexOf('\0') >= 0) {
                throw new ArgumentException("Element name cannot contain null (0x00) characters");
            }
        }
        #endregion

        #region public methods
        public BsonElement Clone() {
            var clone = new BsonElement();
            clone.name = name;
            clone.value = value.Clone();
            return clone;
        }

        public BsonElement DeepClone() {
            var clone = new BsonElement();
            clone.name = name;
            clone.value = value.DeepClone();
            return clone;
        }

        public int CompareTo(
            BsonElement other
        ) {
            if (other == null) { return 1; }
            int r = this.name.CompareTo(other.name);
            if (r != 0) { return r; }
            return this.value.CompareTo(other.value);
        }

        public bool Equals(
            BsonElement rhs
        ) {
            if (rhs == null) { return false; }
            return this.name == rhs.name && this.value == rhs.value;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonElement); // works even if obj is null or of a different type
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + name.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return string.Format("{0}={1}", name, value);
        }
        #endregion

        #region internal methods
        internal void WriteTo(
            BsonWriter bsonWriter
        ) {
            bsonWriter.WriteName(name);
            value.WriteTo(bsonWriter);
        }
        #endregion
    }
}
