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
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Information about a replica set member.
    /// </summary>
    internal sealed class ReplicaSetInformation : IEquatable<ReplicaSetInformation>
    {
        private readonly string _name;
        private readonly MongoServerAddress _primary;
        private readonly List<MongoServerAddress> _members;
        private readonly ReplicaSetTagSet _tagSet;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetInformation"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="primary">The primary.</param>
        /// <param name="members">The members.</param>
        /// <param name="tagSet">The tag set.</param>
        public ReplicaSetInformation(string name, MongoServerAddress primary, IEnumerable<MongoServerAddress> members, ReplicaSetTagSet tagSet)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            
            _name = name;
            _primary = primary;
            _members = members == null ? new List<MongoServerAddress>() : members.ToList();
            _tagSet = tagSet.FrozenCopy();
        }

        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the primary.
        /// </summary>
        public MongoServerAddress Primary
        {
            get { return _primary; }
        }

        /// <summary>
        /// Gets the members.
        /// </summary>
        public IEnumerable<MongoServerAddress> Members
        {
            get { return _members; }
        }

        /// <summary>
        /// Gets the tag set.
        /// </summary>
        public ReplicaSetTagSet TagSet
        {
            get { return _tagSet; }
        }

        // public operators
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ReplicaSetInformation lhs, ReplicaSetInformation rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ReplicaSetInformation lhs, ReplicaSetInformation rhs)
        {
            return !(lhs == rhs);
        }

        // public methods
        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <paramref name="obj"/> parameter is null.
        ///   </exception>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReplicaSetInformation);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="rhs">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="rhs"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(ReplicaSetInformation rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            if (_name != rhs._name)
            {
                return false;
            }

            if (_primary != rhs._primary)
            {
                return false;
            }

            if (_members.Count != rhs._members.Count || _members.Intersect(rhs._members).Count() != _members.Count)
            {
                return false;
            }

            if (_tagSet != rhs._tagSet)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _name.GetHashCode();
            hash = 37 * hash + _primary.GetHashCode();
            foreach (var member in _members)
            {
                hash = 37 * hash + member.GetHashCode();
            }
            hash = 37 * hash + _tagSet.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}: Primary({1}), Members({2}), TagSet({3}", _name, _primary, _members.Count, _tagSet);
        }
    }
}