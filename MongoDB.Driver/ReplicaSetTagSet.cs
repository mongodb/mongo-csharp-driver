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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a set of replica set tags.
    /// </summary>
    public class ReplicaSetTagSet : IEnumerable<ReplicaSetTag>, IEquatable<ReplicaSetTagSet>
    {
        // private fields
        private List<ReplicaSetTag> _tags;
        private ReadOnlyCollection<ReplicaSetTag> _tagsReadOnly;
        private bool _isFrozen;
        private int _frozenHashCode;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ReplicaSetTagSet class.
        /// </summary>
        public ReplicaSetTagSet()
        {
            _tags = new List<ReplicaSetTag>();
            _tagsReadOnly = _tags.AsReadOnly();
        }

        /// <summary>
        /// Initializes a new instance of the ReplicaSetTagSet class.
        /// </summary>
        /// <param name="other">The other ReplicaSetTagSet.</param>
        public ReplicaSetTagSet(ReplicaSetTagSet other)
        {
            _tags = new List<ReplicaSetTag>(other._tags);
            _tagsReadOnly = _tags.AsReadOnly();
        }

        // public properties
        /// <summary>
        /// Gets a count of the number of tags.
        /// </summary>
        public int Count
        {
            get { return _tags.Count; }
        }

        /// <summary>
        /// Gets a read-only collection of the tags.
        /// </summary>
        public ReadOnlyCollection<ReplicaSetTag> Tags
        {
            get { return _tagsReadOnly; }
        }

        // public operators
        /// <summary>
        /// Determines whether two specified ReplicaSetTagSet objects have different values.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is different from the value of rhs; otherwise, false.</returns>
        public static bool operator !=(ReplicaSetTagSet lhs, ReplicaSetTagSet rhs)
        {
            return !ReplicaSetTagSet.Equals(lhs, rhs);
        }

        /// <summary>
        /// Determines whether two specified ReplicaSetTagSet objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool operator ==(ReplicaSetTagSet lhs, ReplicaSetTagSet rhs)
        {
            return ReplicaSetTagSet.Equals(lhs, rhs);
        }

        // public static methods
        /// <summary>
        /// Determines whether two specified ReplicaSetTagSet objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool Equals(ReplicaSetTagSet lhs, ReplicaSetTagSet rhs)
        {
            if ((object)lhs == null) { return (object)rhs == null; }
            return lhs.Equals(rhs);
        }

        // public methods
        /// <summary>
        /// Adds a tag to the list.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>The ReplicaSetTagSet so calls to Add can be chained.</returns>
        public ReplicaSetTagSet Add(ReplicaSetTag tag)
        {
            if (_isFrozen) { ThrowFrozenException(); }
            _tags.Add(tag);
            return this;
        }

        /// <summary>
        /// Adds a tag to the list.
        /// </summary>
        /// <param name="name">The name of the tag.</param>
        /// <param name="value">The value of the tag.</param>
        /// <returns>The ReplicaSetTagSet so calls to Add can be chained.</returns>
        public ReplicaSetTagSet Add(string name, string value)
        {
            if (_isFrozen) { ThrowFrozenException(); }
            _tags.Add(new ReplicaSetTag(name, value));
            return this;
        }

        /// <summary>
        /// Creates a clone of the ReplicaSetTagSet.
        /// </summary>
        /// <returns>A clone of the ReplicaSetTagSet.</returns>
        public ReplicaSetTagSet Clone()
        {
            return new ReplicaSetTagSet(this);
        }

        /// <summary>
        /// Determines whether this instance and another specified ReplicaSetTagSet object have the same value.
        /// </summary>
        /// <param name="rhs">The ReplicaSetTagSet object to compare to this instance.</param>
        /// <returns>True if the value of the rhs parameter is the same as this instance; otherwise, false.</returns>
        public bool Equals(ReplicaSetTagSet rhs)
        {
            if ((object)rhs == null || GetType() != rhs.GetType()) { return false; }
            if ((object)this == (object)rhs) { return true; }
            return _tags.SequenceEqual(rhs._tags);
        }

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a ReplicaSetTagSet object, have the same value.
        /// </summary>
        /// <param name="obj">The ReplicaSetTagSet object to compare to this instance.</param>
        /// <returns>True if obj is a ReplicaSetTagSet object and its value is the same as this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReplicaSetTagSet); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Freezes the ReplicaSetTagSet.
        /// </summary>
        /// <returns>The frozen ReplicaSetTagSet.</returns>
        public ReplicaSetTagSet Freeze()
        {
            if (!_isFrozen)
            {
                _frozenHashCode = GetHashCode();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the ReplicaSetTagSet.
        /// </summary>
        /// <returns>A frozen copy of the ReplicaSetTagSet.</returns>
        public ReplicaSetTagSet FrozenCopy()
        {
            if (_isFrozen)
            {
                return this;
            }
            else
            {
                return Clone().Freeze();
            }
        }

        /// <summary>
        /// Returns the hash code for this ReplicaSetTagSet object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            return _tags.GetHashCode();
        }

        /// <summary>
        /// Gets whether the tag set has been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Tests whether this tag set matches a server instance.
        /// </summary>
        /// <param name="instance">The server instance.</param>
        /// <returns>True if every tag in this tag set is also in the server instance tag set; otherwise, false.</returns>
        public bool MatchesInstance(MongoServerInstance instance)
        {
            // an empty tag set matches anything
            if (instance.InstanceType != MongoServerInstanceType.ReplicaSetMember || _tags.Count == 0)
            {
                return true;
            }

            var tagSet = instance.ReplicaSetInformation.TagSet;
            foreach (var tag in _tags)
            {
                if (!tagSet.Contains(tag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a string representation of the ReplicaSetTagSet.
        /// </summary>
        /// <returns>A string representation of the user.</returns>
        public override string ToString()
        {
            return string.Format("{{{0}}}", string.Join(", ", _tags.Select(t => t.ToString()).ToArray()));
        }

        // private methods
        private void ThrowFrozenException()
        {
            throw new InvalidOperationException("ReplicaSetTagSet has been frozen and no further changes are allowed.");
        }

        // explicit interface implementations
        IEnumerator<ReplicaSetTag> IEnumerable<ReplicaSetTag>.GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tags.GetEnumerator();
        }
    }
}
