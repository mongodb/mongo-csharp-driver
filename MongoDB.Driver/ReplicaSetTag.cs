/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a replica set tag.
    /// </summary>
    public class ReplicaSetTag : IEquatable<ReplicaSetTag>
    {
        // private fields
        private string _name;
        private string _value;
        private int _hashCode;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ReplicaSetTag class.
        /// </summary>
        /// <param name="name">The name of the tag.</param>
        /// <param name="value">The value of the tag.</param>
        public ReplicaSetTag(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _name = name;
            _value = value;
            _hashCode = GetHashCodeHelper();
        }

        // public properties
        /// <summary>
        /// Gets the name of the tag.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the value of the tag.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        // public operators
        /// <summary>
        /// Determines whether two specified ReplicaSetTag objects have different values.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is different from the value of rhs; otherwise, false.</returns>
        public static bool operator !=(ReplicaSetTag lhs, ReplicaSetTag rhs)
        {
            return !ReplicaSetTag.Equals(lhs, rhs);
        }

        /// <summary>
        /// Determines whether two specified ReplicaSetTag objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool operator ==(ReplicaSetTag lhs, ReplicaSetTag rhs)
        {
            return ReplicaSetTag.Equals(lhs, rhs);
        }

        // public static methods
        /// <summary>
        /// Determines whether two specified ReplicaSetTag objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool Equals(ReplicaSetTag lhs, ReplicaSetTag rhs)
        {
            if ((object)lhs == null) { return (object)rhs == null; }
            return lhs.Equals(rhs);
        }

        // public methods
        /// <summary>
        /// Determines whether this instance and another specified ReplicaSetTag object have the same value.
        /// </summary>
        /// <param name="rhs">The ReplicaSetTag object to compare to this instance.</param>
        /// <returns>True if the value of the rhs parameter is the same as this instance; otherwise, false.</returns>
        public bool Equals(ReplicaSetTag rhs)
        {
            if ((object)rhs == null || GetType() != rhs.GetType()) { return false; }
            if ((object)this == (object)rhs) { return true; }
            return _hashCode == rhs._hashCode && _name == rhs._name && _value == rhs._value;
        }

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a ReplicaSetTag object, have the same value.
        /// </summary>
        /// <param name="obj">The ReplicaSetTag object to compare to this instance.</param>
        /// <returns>True if obj is a ReplicaSetTag object and its value is the same as this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReplicaSetTag); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Returns the hash code for this ReplicaSetTag object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Returns a string representation of the credentials.
        /// </summary>
        /// <returns>A string representation of the user.</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", _name, _value);
        }

        // private methods
        private int GetHashCodeHelper()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _name.GetHashCode();
            hash = 37 * hash + _value.GetHashCode();
            return hash;
        }
    }
}
