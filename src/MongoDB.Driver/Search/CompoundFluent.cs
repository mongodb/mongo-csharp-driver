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

using System.Collections.Generic;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Fluent interface for compound search definitions.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class CompoundFluent<TDocument>
    {
        /// <summary>
        /// Adds clauses which must match to produce results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public abstract CompoundFluent<TDocument> Must(IEnumerable<SearchDefinition<TDocument>> clauses);

        /// <summary>
        /// Adds clauses which must match to produce results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public CompoundFluent<TDocument> Must(params SearchDefinition<TDocument>[] clauses) =>
            Must((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Adds clauses which must not match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public abstract CompoundFluent<TDocument> MustNot(IEnumerable<SearchDefinition<TDocument>> clauses);

        /// <summary>
        /// Adds clauses which must not match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public CompoundFluent<TDocument> MustNot(params SearchDefinition<TDocument>[] clauses) =>
            MustNot((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Adds clauses which cause documents in the result set to be scored higher if
        /// they match.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public abstract CompoundFluent<TDocument> Should(IEnumerable<SearchDefinition<TDocument>> clauses);

        /// <summary>
        /// Adds clauses which cause documents in the result set to be scored higher if
        /// they match.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public CompoundFluent<TDocument> Should(params SearchDefinition<TDocument>[] clauses) =>
            Should((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Adds clauses which must all match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public abstract CompoundFluent<TDocument> Filter(IEnumerable<SearchDefinition<TDocument>> clauses);

        /// <summary>
        /// Adds clauses which must all match for a document to be included in the
        /// results.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>The compound fluent interface.</returns>
        public CompoundFluent<TDocument> Filter(params SearchDefinition<TDocument>[] clauses) =>
            Filter((IEnumerable<SearchDefinition<TDocument>>)clauses);

        /// <summary>
        /// Sets a value specifying the minimum number of should clauses the must match
        /// to include a document in the results.
        /// </summary>
        /// <param name="minimumShouldMatch">The value to set.</param>
        /// <returns>The compound fluent interface.</returns>
        public abstract CompoundFluent<TDocument> MinimumShouldMatch(int minimumShouldMatch);

        /// <summary>
        /// Constructs a search definition from the fluent interface.
        /// </summary>
        /// <returns>A compound search definition.</returns>
        public abstract SearchDefinition<TDocument> ToSearchDefinition();

        /// <summary>
        /// Performs an implicit conversion from a <see cref="CompoundFluent{TDocument}"/>
        /// to a <see cref="SearchDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="compound">The compound fluent interface.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator SearchDefinition<TDocument>(CompoundFluent<TDocument> compound) =>
            compound.ToSearchDefinition();
    }
}
