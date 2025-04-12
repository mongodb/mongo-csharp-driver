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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Represents the scoreDetails object for a document from a $rankFusion result.
    /// </summary>
    public sealed class RankFusionScoreDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RankFusionScoreDetails"/> class.
        /// </summary>
        /// <param name="value">The computed score which is the same as the score available via {$meta: "score"}.</param>
        /// <param name="description">Description of how the score was computed.</param>
        /// <param name="details">Info about how each input pipeline in the rankFusion stage contributed to the computed score.</param>
        /// <seealso cref="IAggregateFluentExtensions.RankFusion{TResult, TNewResult}(IAggregateFluent{TResult}, Dictionary{string,PipelineDefinition{TResult,TNewResult}}, Dictionary{string,double}, RankFusionOptions{TNewResult})"/>
        public RankFusionScoreDetails(double value, string description, BsonDocument[] details)
        {
            Value = value;
            Description = description;
            Details = details;
        }

        /// <summary>
        /// Gets the computed score which is the same as the score available via {$meta: "score"}.
        /// </summary>
        [BsonElement("value")]
        public double Value { get; }

        /// <summary>
        /// Gets the description of how the score was computed.
        /// </summary>
        [BsonElement("description")]
        public string Description { get; }

        /// <summary>
        /// Gets info about how each input pipeline in the rankFusion stage contributed to the computed score.
        /// </summary>
        [BsonDefaultValue(null)]
        [BsonElement("details")]
        public BsonDocument[] Details { get; }
    }
}