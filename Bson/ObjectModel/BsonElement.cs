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
    /// <summary>
    /// Represents a BSON element.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the BsonElement class.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
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
        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        public string Name {
            get { return name; }
        }

        /// <summary>
        /// Gets or sets the value of the element.
        /// </summary>
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
        /// <summary>
        /// Compares two BsonElements.
        /// </summary>
        /// <param name="lhs">The first BsonElement.</param>
        /// <param name="rhs">The other BsonElement.</param>
        /// <returns>True if the two BsonElements are equal (or both null).</returns>
        public static bool operator ==(
            BsonElement lhs,
            BsonElement rhs
        ) {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two BsonElements.
        /// </summary>
        /// <param name="lhs">The first BsonElement.</param>
        /// <param name="rhs">The other BsonElement.</param>
        /// <returns>True if the two BsonElements are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(
            BsonElement lhs,
            BsonElement rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new instance of the BsonElement class.
        /// </summary>
        /// <param name="condition">Whether to create the BsonElement or return null.</param>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>A BsonElement or null.</returns>
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

        /// <summary>
        /// Creates a new instance of the BsonElement class.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>A BsonElement or null.</returns>
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
                    string message = string.Format("Expected element '{0}', not '{1}'.", expectedName, element.name);
                    throw new FileFormatException(message);
                }
                return element;
            } else {
                string message = string.Format("Element '{0}' is missing.", expectedName);
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
        /// <summary>
        /// Creates a shallow clone of the element (see also DeepClone).
        /// </summary>
        /// <returns>A shallow clone of the element.</returns>
        public BsonElement Clone() {
            return new BsonElement(name, value.Clone());
        }

        /// <summary>
        /// Creates a deep clone of the element (see also Clone).
        /// </summary>
        /// <returns>A deep clone of the element.</returns>
        public BsonElement DeepClone() {
            var clone = new BsonElement();
            clone.name = name;
            clone.value = value.DeepClone();
            return clone;
        }

        /// <summary>
        /// Compares this BsonElement to another BsonElement.
        /// </summary>
        /// <param name="other">The other BsonElement.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonElement is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            BsonElement other
        ) {
            if (other == null) { return 1; }
            int r = this.name.CompareTo(other.name);
            if (r != 0) { return r; }
            return this.value.CompareTo(other.value);
        }

        /// <summary>
        /// Compares this BsonElement to another BsonElement.
        /// </summary>
        /// <param name="rhs">The other BsonElement.</param>
        /// <returns>True if the two BsonElement values are equal.</returns>
        public bool Equals(
            BsonElement rhs
        ) {
            if (rhs == null) { return false; }
            return this.name == rhs.name && this.value == rhs.value;
        }

        /// <summary>
        /// Compares this BsonElement to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonElement and equal to this one.</returns>
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonElement); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + name.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
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
