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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Base class for search queries.
    /// </summary>
    public abstract class SearchQueryDefinition
    {
        /// <summary>
        /// Renders the query to a <see cref="BsonValue"/>.
        /// </summary>
        /// <returns>A <see cref="BsonValue"/>.</returns>
        public abstract BsonValue Render();

        /// <summary>
        /// Performs an implicit conversion from a string to <see cref="SearchQueryDefinition"/>.
        /// </summary>
        /// <param name="query">The string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchQueryDefinition(string query) =>
            new SingleSearchQueryDefinition(query);

        /// <summary>
        /// Performs an implicit conversion from an array of strings to <see cref="SearchQueryDefinition"/>.
        /// </summary>
        /// <param name="queries">The array of strings.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchQueryDefinition(string[] queries) =>
            new MultiSearchQueryDefinition(queries);

        /// <summary>
        /// Performs an implicit conversion from a list of strings to <see cref="SearchQueryDefinition"/>.
        /// </summary>
        /// <param name="queries">The list of strings.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchQueryDefinition(List<string> queries) =>
            new MultiSearchQueryDefinition(queries);
    }

    /// <summary>
    /// A query definition for a single string.
    /// </summary>
    public sealed class SingleSearchQueryDefinition : SearchQueryDefinition
    {
        private readonly string _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleSearchQueryDefinition"/> class.
        /// </summary>
        /// <param name="query">The query string.</param>
        public SingleSearchQueryDefinition(string query)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
        }

        /// <inheritdoc />
        public override BsonValue Render() => new BsonString(_query);
    }

    /// <summary>
    /// A query definition for multiple strings.
    /// </summary>
    public sealed class MultiSearchQueryDefinition : SearchQueryDefinition
    {
        private readonly string[] _queries;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSearchQueryDefinition"/> class.
        /// </summary>
        /// <param name="queries">The query strings.</param>
        public MultiSearchQueryDefinition(IEnumerable<string> queries)
        {
            _queries = Ensure.IsNotNull(queries, nameof(queries)).ToArray();
        }

        /// <inheritdoc/>
        public override BsonValue Render() => new BsonArray(_queries);
    }
}
