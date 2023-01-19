/* Copyright 2010-present MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Options for fuzzy search.
    /// </summary>
    public sealed class SearchFuzzyOptions
    {
        private int? _maxEdits;
        private int? _maxExpansions;
        private int? _prefixLength;

        /// <summary>
        /// Gets or sets the maximum number of single-character edits required to match the
        /// specified search term.
        /// </summary>
        public int? MaxEdits
        {
            get => _maxEdits;
            set => _maxEdits = Ensure.IsNullOrBetween(value, 1, 2, nameof(value));
        }

        /// <summary>
        /// Gets or sets the number of variations to generate and search for.
        /// </summary>
        public int? MaxExpansions
        {
            get => _maxExpansions;
            set => _maxExpansions = Ensure.IsNullOrGreaterThanZero(value, nameof(value));
        }

        /// <summary>
        /// Gets or sets the number of characters at the beginning of each term in the result that
        /// must exactly match.
        /// </summary>
        public int? PrefixLength
        {
            get => _prefixLength;
            set => _prefixLength = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value));
        }

        internal BsonDocument Render()
            => new()
            {
                { "maxEdits", _maxEdits, _maxEdits != null },
                { "prefixLength", _prefixLength, _prefixLength != null },
                { "maxExpansions", _maxExpansions, _maxExpansions != null }
            };
    }
}
