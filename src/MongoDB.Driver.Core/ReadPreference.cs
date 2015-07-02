/* Copyright 2013-2015 MongoDB Inc.
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
        private static readonly ReadPreference __nearest = new ReadPreference(ReadPreferenceMode.Nearest);
        private static readonly ReadPreference __primary = new ReadPreference(ReadPreferenceMode.Primary);
        private static readonly ReadPreference __primaryPreferred = new ReadPreference(ReadPreferenceMode.PrimaryPreferred);
        private static readonly ReadPreference __secondary = new ReadPreference(ReadPreferenceMode.Secondary);
        private static readonly ReadPreference __secondaryPreferred = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

        // static properties
        /// <summary>
        /// Gets an instance of ReadPreference that represents a Nearest read preference.
        /// </summary>
        /// <value>
        /// An instance of ReadPreference that represents a Nearest read preference.
        /// </value>
        public static ReadPreference Nearest
        {
            get { return __nearest; }
        }

        /// <summary>
        /// Gets an instance of ReadPreference that represents a Primary read preference.
        /// </summary>
        /// <value>
        /// An instance of ReadPreference that represents a Primary read preference.
        /// </value>
        public static ReadPreference Primary
        {
            get { return __primary; }
        }

        /// <summary>
        /// Gets an instance of ReadPreference that represents a PrimaryPreferred read preference.
        /// </summary>
        /// <value>
        /// An instance of ReadPreference that represents a PrimaryPreferred read preference.
        /// </value>
        public static ReadPreference PrimaryPreferred
        {
            get { return __primaryPreferred; }
        }

        /// <summary>
        /// Gets an instance of ReadPreference that represents a Secondary read preference.
        /// </summary>
        /// <value>
        /// An instance of ReadPreference that represents a Secondary read preference.
        /// </value>
        public static ReadPreference Secondary
        {
            get { return __secondary; }
        }

        /// <summary>
        /// Gets an instance of ReadPreference that represents a SecondaryPreferred read preference.
        /// </summary>
        /// <value>
        /// An instance of ReadPreference that represents a SecondaryPreferred read preference.
        /// </value>
        public static ReadPreference SecondaryPreferred
        {
            get { return __secondaryPreferred; }
        }
        #endregion

        // fields
        private readonly ReadPreferenceMode _mode;
        private readonly IReadOnlyList<TagSet> _tagSets;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadPreference"/> class.
        /// </summary>
        /// <param name="mode">The read preference mode.</param>
        /// <param name="tagSets">The tag sets.</param>
        public ReadPreference(
            ReadPreferenceMode mode,
            IEnumerable<TagSet> tagSets = null)
        {
            _mode = mode;
            _tagSets = (tagSets ?? Enumerable.Empty<TagSet>()).ToList();

            if (_mode == ReadPreferenceMode.Primary && _tagSets.Count() > 0)
            {
                throw new ArgumentException("TagSets cannot be used with ReadPreferenceMode Primary.", "tagSets");
            }
        }

        // properties
        /// <summary>
        /// Gets the read preference mode.
        /// </summary>
        /// <value>
        /// The read preference mode.
        /// </value>
        public ReadPreferenceMode ReadPreferenceMode
        {
            get { return _mode; }
        }

        /// <summary>
        /// Gets the tag sets.
        /// </summary>
        /// <value>
        /// The tag sets.
        /// </value>
        public IReadOnlyList<TagSet> TagSets
        {
            get { return _tagSets; }
        }

        // methods
        /// <inheritdoc/>
        public bool Equals(ReadPreference other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _mode == other._mode &&
                _tagSets.SequenceEqual(other.TagSets);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReadPreference);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_mode)
                .HashElements(_tagSets)
                .GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{{ Mode = {0}, TagSets = [{1}] }}", _mode, string.Join(", ", _tagSets));
        }

        /// <summary>
        /// Returns a new instance of ReadPreference with some values changed.
        /// </summary>
        /// <param name="mode">The read preference mode.</param>
        /// <returns>A new instance of ReadPreference.</returns>
        public ReadPreference With(ReadPreferenceMode mode)
        {
            return new ReadPreference(mode, _tagSets);
        }

        /// <summary>
        /// Returns a new instance of ReadPreference with some values changed.
        /// </summary>
        /// <param name="tagSets">The tag sets.</param>
        /// <returns>A new instance of ReadPreference.</returns>
        public ReadPreference With(IEnumerable<TagSet> tagSets)
        {
            return new ReadPreference(_mode, tagSets);
        }
    }
}
