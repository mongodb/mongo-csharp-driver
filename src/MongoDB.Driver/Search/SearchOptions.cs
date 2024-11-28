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
    /// Options for search.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchOptions<TDocument>
    {
        /// <summary>
        /// Gets or sets the options for counting the search results.
        /// </summary>
        public SearchCountOptions CountOptions { get; set; }

        /// <summary>
        /// Gets or sets the options for highlighting.
        /// </summary>
        public SearchHighlightOptions<TDocument> Highlight { get; set; }

        /// <summary>
        /// Gets or sets the index name.
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// Gets or sets a flag that specifies whether to perform a full document lookup on the backend database
        /// or return only stored source fields directly from Atlas Search.
        /// </summary>
        public bool ReturnStoredSource { get; set; }

        /// <summary>
        /// Gets or sets a flag that specifies whether to return a detailed breakdown
        /// of the score for each document in the result.
        /// </summary>
        public bool ScoreDetails { get; set; }

        /// <summary>
        /// Gets or sets the sort specification.
        /// </summary>
        public SortDefinition<TDocument> Sort { get; set; }

        /// <summary>
        /// Gets or sets the options for tracking search terms.
        /// </summary>
        public SearchTrackingOptions Tracking { get; set; }

        /// <summary>
        /// Gets or sets the "after" reference point for pagination.
        /// When set, the search retrieves documents starting immediately after the specified reference point.
        /// </summary>
        public string SearchAfter { get; set; }

        /// <summary>
        /// Gets or sets the "before" reference point for pagination.
        /// When set, the search retrieves documents starting immediately before the specified reference point.
        /// </summary>
        public string SearchBefore { get; set; }
    }
}
