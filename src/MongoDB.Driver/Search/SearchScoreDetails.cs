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
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Represents the scoreDetails object for a document in the result.
    /// </summary>
    public sealed class SearchScoreDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchScoreDetails"/> class.
        /// </summary>
        /// <param name="value">Contribution towards the score by a subset of the scoring formula.</param>
        /// <param name="description">Subset of the scoring formula.</param>
        /// <param name="details">Breakdown of the score for each match in the document.</param>
        public SearchScoreDetails(double value, string description, SearchScoreDetails[] details)
        {
            Value = value;
            Description = description;
            Details = details;
        }

        /// <summary>
        /// Gets the contribution towards the score by a subset of the scoring formula.
        /// </summary>
        [BsonElement("value")]
        public double Value { get; }

        /// <summary>
        /// Gets the subset of the scoring formula including details about how the document
        /// was scored and factors considered in calculating the score.
        /// </summary>
        [BsonElement("description")]
        public string Description { get; }

        /// <summary>
        /// Breakdown of the score for each match in the document based on the subset of the scoring formula.
        /// (if any).
        /// </summary>
        [BsonDefaultValue(null)]
        [BsonElement("details")]
        public SearchScoreDetails[] Details { get; }
    }
}
