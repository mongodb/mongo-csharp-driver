// Copyright 2010-present MongoDB Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for <see cref="IAggregateFluent{TResult}"/>.
    /// </summary>
    public static class AggregateFluentBuilder
    {
        /// <summary>
        /// Appends a $search stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="query">The search definition.</param>
        /// <param name="highlight">The highlight options.</param>
        /// <param name="indexName">The index name.</param>
        /// <param name="count">The count options.</param>
        /// <param name="returnStoredSource">
        /// Flag that specifies whether to perform a full document lookup on the backend database
        /// or return only stored source fields directly from Atlas Search.
        /// </param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TResult> Search<TResult>(
            this IAggregateFluent<TResult> aggregate,
            SearchDefinition<TResult> query,
            HighlightOptions<TResult> highlight = null,
            string indexName = null,
            SearchCountOptions count = null,
            bool returnStoredSource = false)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            return aggregate.AppendStage(PipelineStageDefinitionBuilder.Search(query, highlight, indexName, count, returnStoredSource));
        }
    }
}
