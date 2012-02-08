﻿/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a BSON element.
    /// </summary>
    [Serializable]
    public class BsonElement : IComparable<BsonElement>, IEquatable<BsonElement>
    {
        // private fields
        private string _name;
        private BsonValue _value;

        // constructors
        // NOTE: for every public BsonElement constructor there is a matching constructor, Add and Set method in BsonDocument

        // used when cloning an existing element, caller will set name and value
        private BsonElement()
        {
        }

        /// <summary>
        /// Initializes a new instance of the BsonElement class.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        public BsonElement(string name, BsonValue value)
        {
            ValidateElementName(name);
            _name = name;
            _value = value;
        }

        // public properties
        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets or sets the value of the element.
        /// </summary>
        public BsonValue Value
        {
            get { return _value; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _value = value;
            }
        }

        // public operators
        /// <summary>
        /// Compares two BsonElements.
        /// </summary>
        /// <param name="lhs">The first BsonElement.</param>
        /// <param name="rhs">The other BsonElement.</param>
        /// <returns>True if the two BsonElements are equal (or both null).</returns>
        public static bool operator ==(BsonElement lhs, BsonElement rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two BsonElements.
        /// </summary>
        /// <param name="lhs">The first BsonElement.</param>
        /// <param name="rhs">The other BsonElement.</param>
        /// <returns>True if the two BsonElements are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(BsonElement lhs, BsonElement rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the BsonElement class.
        /// </summary>
        /// <param name="condition">Whether to create the BsonElement or return null.</param>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>A BsonElement or null.</returns>
        public static BsonElement Create(bool condition, string name, BsonValue value)
        {
            if (condition && value != null)
            {
                return new BsonElement(name, value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of the BsonElement class.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>A BsonElement or null.</returns>
        public static BsonElement Create(string name, BsonValue value)
        {
            if (value != null)
            {
                return new BsonElement(name, value);
            }
            else
            {
                return null;
            }
        }

        // internal static methods
        internal static bool ReadFrom(BsonReader bsonReader, out BsonElement element)
        {
            BsonType bsonType;
            if ((bsonType = bsonReader.ReadBsonType()) != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                var value = BsonValue.ReadFrom(bsonReader);
                element = new BsonElement(name, value);
                return true;
            }
            else
            {
                element = null;
                return false;
            }
        }

        internal static BsonElement ReadFrom(BsonReader bsonReader, string expectedName)
        {
            BsonElement element;
            if (ReadFrom(bsonReader, out element))
            {
                if (element.Name != expectedName)
                {
                    string message = string.Format("Expected element '{0}', not '{1}'.", expectedName, element._name);
                    throw new FileFormatException(message);
                }
                return element;
            }
            else
            {
                string message = string.Format("Element '{0}' is missing.", expectedName);
                throw new FileFormatException(message);
            }
        }

        // private static methods
        private static void ValidateElementName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (name.IndexOf('\0') >= 0)
            {
                throw new ArgumentException("Element name cannot contain null (0x00) characters");
            }
        }

        // public methods
        /// <summary>
        /// Creates a shallow clone of the element (see also DeepClone).
        /// </summary>
        /// <returns>A shallow clone of the element.</returns>
        public BsonElement Clone()
        {
            return new BsonElement(_name, _value.Clone());
        }

        /// <summary>
        /// Creates a deep clone of the element (see also Clone).
        /// </summary>
        /// <returns>A deep clone of the element.</returns>
        public BsonElement DeepClone()
        {
            var clone = new BsonElement();
            clone._name = _name;
            clone._value = _value.DeepClone();
            return clone;
        }

        /// <summary>
        /// Compares this BsonElement to another BsonElement.
        /// </summary>
        /// <param name="other">The other BsonElement.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonElement is less than, equal to, or greather than the other.</returns>
        public int CompareTo(BsonElement other)
        {
            if (other == null) { return 1; }
            int r = _name.CompareTo(other._name);
            if (r != 0) { return r; }
            return _value.CompareTo(other._value);
        }

        /// <summary>
        /// Compares this BsonElement to another BsonElement.
        /// </summary>
        /// <param name="rhs">The other BsonElement.</param>
        /// <returns>True if the two BsonElement values are equal.</returns>
        public bool Equals(BsonElement rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return _name == rhs._name && _value == rhs._value;
        }

        /// <summary>
        /// Compares this BsonElement to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonElement and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BsonElement); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _name.GetHashCode();
            hash = 37 * hash + _value.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            return string.Format("{0}={1}", _name, _value);
        }

        // internal methods
        internal void WriteTo(BsonWriter bsonWriter)
        {
            bsonWriter.WriteName(_name);
            _value.WriteTo(bsonWriter);
        }
    }
}
