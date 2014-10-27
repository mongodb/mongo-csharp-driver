/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a read preference.
    /// </summary>
    public sealed class ReadPreference : IEquatable<ReadPreference>
    {
        #region static
        // static fields
        private static readonly IReadOnlyList<TagSet> __noTagSets = new TagSet[0];
        private static readonly ReadPreference __nearest = new ReadPreference(ReadPreferenceMode.Nearest);
        private static readonly ReadPreference __primary = new ReadPreference(ReadPreferenceMode.Primary);
        private static readonly ReadPreference __primaryPreferred = new ReadPreference(ReadPreferenceMode.PrimaryPreferred);
        private static readonly ReadPreference __secondary = new ReadPreference(ReadPreferenceMode.Secondary);
        private static readonly ReadPreference __secondaryPreferred = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

        // static properties
        public static ReadPreference Nearest
        {
            get { return __nearest; }
        }

        public static ReadPreference Primary
        {
            get { return __primary; }
        }

        public static ReadPreference PrimaryPreferred
        {
            get { return __primaryPreferred; }
        }

        public static ReadPreference Secondary
        {
            get { return __secondary; }
        }

        public static ReadPreference SecondaryPreferred
        {
            get { return __secondaryPreferred; }
        }
        #endregion

        // fields
        private readonly ReadPreferenceMode _mode;
        private readonly IReadOnlyList<TagSet> _tagSets;

        // constructors
        public ReadPreference(ReadPreferenceMode mode)
        {
            _mode = mode;
            _tagSets = __noTagSets;
        }

        public ReadPreference(ReadPreferenceMode mode, IEnumerable<TagSet> tagSets)
        {
            Ensure.IsNotNull(tagSets, "tagSets");
            if (mode == ReadPreferenceMode.Primary && tagSets.Count() > 0)
            {
                throw new ArgumentException("TagSets cannot be used with ReadPreferenceMode Primary.", "tagSets");
            }

            _mode = mode;
            _tagSets = tagSets.ToList();
        }

        // properties
        public ReadPreferenceMode ReadPreferenceMode
        {
            get { return _mode; }
        }

        public IReadOnlyList<TagSet> TagSets
        {
            get { return _tagSets; }
        }

        // methods
        public bool Equals(ReadPreference other)
        {
            if (other == null)
            {
                return false;
            }

            return _mode == other._mode &&
                _tagSets.SequenceEqual(other.TagSets);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ReadPreference);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_mode)
                .HashElements(_tagSets)
                .GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{ Mode = {0}, TagSets = {1} }}", _mode, _tagSets);
        }

        public ReadPreference WithMode(ReadPreferenceMode value)
        {
            return (_mode == value) ? this : new ReadPreference(value, _tagSets);
        }

        public ReadPreference WithTagSets(IEnumerable<TagSet> value)
        {
            if (object.ReferenceEquals(_tagSets, value))
            {
                return this;
            }
            if (!object.ReferenceEquals(_tagSets, null) && !object.ReferenceEquals(value, null) && _tagSets.SequenceEqual(value))
            {
                return this;
            }
            return new ReadPreference(_mode, value);
        }
    }
}
