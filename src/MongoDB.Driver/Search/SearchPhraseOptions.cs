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

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Options for atlas search phrase operator.
    /// </summary>
    public sealed class SearchPhraseOptions<TDocument>
    {
        /// <summary>
        /// The score modifier.
        /// </summary>
        public SearchScoreDefinition<TDocument> Score { get; set; }

        /// <summary>
        /// The allowable distance between words in the query phrase.
        /// </summary>
        public int? Slop { get; set; }

        /// <summary>
        /// The name of the synonym mapping definition in the index definition. Value can't be an empty string (e.g. "").
        /// </summary>
        public string Synonyms { get; set; }
    }
}