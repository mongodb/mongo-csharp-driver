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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A builder for compound search definitions.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class CompoundSearchDefinitionBuilder<TDocument>
    {
        private List<SearchDefinition<TDocument>> _must;
        private List<SearchDefinition<TDocument>> _mustNot;
        private List<SearchDefinition<TDocument>> _should;
        private List<SearchDefinition<TDocument>> _filter;
        private int _minimumShouldMatch = 0;
        private SearchScoreDefinition<TDocument> _score;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundSearchDefinitionBuilder{TDocument}"/> class.
        /// </summary>
        /// <param name="score"></param>
        public CompoundSearchDefinitionBuilder(SearchScoreDefinition<TDocument> score = null)
        {
            _score = score;
        }

        /// <summary>
        /// Adds clauses which must match to produce results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> Must(IEnumerable<SearchDefinition<TDocument>> clauses) =>
             AddClauses(ref _must, clauses);

        /// <summary>
        /// Adds clauses which must match to produce results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> Must(params SearchDefinition<TDocument>[] clauses) =>
            Must((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Adds clauses which must not match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> MustNot(IEnumerable<SearchDefinition<TDocument>> clauses) =>
            AddClauses(ref _mustNot, clauses);

        /// <summary>
        /// Adds clauses which must not match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> MustNot(params SearchDefinition<TDocument>[] clauses) =>
            MustNot((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Adds clauses which cause documents in the result set to be scored higher if
        /// they match.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> Should(IEnumerable<SearchDefinition<TDocument>> clauses) =>
            AddClauses(ref _should, clauses);

        /// <summary>
        /// Adds clauses which cause documents in the result set to be scored higher if
        /// they match.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> Should(params SearchDefinition<TDocument>[] clauses) =>
            Should((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Adds clauses which must all match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> Filter(IEnumerable<SearchDefinition<TDocument>> clauses) =>
             AddClauses(ref _filter, clauses);

        /// <summary>
        /// Adds clauses which must all match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> Filter(params SearchDefinition<TDocument>[] clauses) =>
            Filter((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Sets a value specifying the minimum number of should clauses the must match
        /// to include a document in the results.
        /// </summary>
        /// <param name="minimumShouldMatch">The value to set.</param>
        /// <returns>The compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> MinimumShouldMatch(int minimumShouldMatch)
        {
            _minimumShouldMatch = minimumShouldMatch;
            return this;
        }

        /// <summary>
        /// Constructs a search definition from the builder.
        /// </summary>
        /// <returns>A compound search definition.</returns>
        public SearchDefinition<TDocument> ToSearchDefinition() =>
            new CompoundSearchDefinition<TDocument>(_must, _mustNot, _should, _filter, _minimumShouldMatch, _score);

        /// <summary>
        /// Performs an implicit conversion from a <see cref="CompoundSearchDefinitionBuilder{TDocument}"/>
        /// to a <see cref="SearchDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="builder">The compound search definition builder.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator SearchDefinition<TDocument>(CompoundSearchDefinitionBuilder<TDocument> builder) =>
            builder.ToSearchDefinition();

        private CompoundSearchDefinitionBuilder<TDocument> AddClauses(ref List<SearchDefinition<TDocument>> clauses, IEnumerable<SearchDefinition<TDocument>> newClauses)
        {
            Ensure.IsNotNull(newClauses, nameof(newClauses));
            (clauses ??= new()).AddRange(newClauses);

            return this;
        }
    }
}
